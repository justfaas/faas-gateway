using System.Net.Http.Headers;
using System.Net.Sockets;

internal class ProxyHttpClient
{
    private readonly HttpClient httpClient;
    private readonly IGatewayMetrics metrics;

    public ProxyHttpClient( IHttpClientFactory httpClientFactory, IGatewayMetrics proxyMetrics )
    {
        httpClient = httpClientFactory.CreateClient( "proxy" );
        metrics = proxyMetrics;
    }

    public async Task<HttpResponseMessage> ExecuteAsync( 
          HttpRequest source
        , string serviceNamespace
        , string serviceName
        , int servicePort
        , string path
        , CancellationToken cancellationToken )
    {
        var url = $"http://{serviceName}.{serviceNamespace}.svc.cluster.local:{servicePort}{path}";
        var requestTimeout = TimeSpan.FromSeconds( 30 );

        if ( source.Headers.TryGetValue( "X-Function-Timeout", out var timeoutDuration ) )
        {
            requestTimeout = TimeSpanHelper.ParseDuration( timeoutDuration.ToString() ) ?? requestTimeout;
        }

        #if DEBUG

        if ( Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" )?.Equals( "Development" ) == true )
        {
            // override using local kubernetes
            // if the port is different, use the X-Service-Port header to override the default
            url = $"http://localhost:{servicePort}{path}";
        }

        #endif

        HttpResponseMessage? response = null;
        var isConnectionError = false;
        var taskExecute = async () =>
        {
            // rewrite request message
            var message = await source.RewriteProxyRequestAsync( url );

            // update function service metrics
            metrics.ProxyRequestsTotal( serviceNamespace, serviceName ).Inc();

            // send request
            try
            {
                isConnectionError = false;
                using ( var tokenSource = new CancellationTokenSource( requestTimeout ) )
                {
                    response = await httpClient.SendAsync( message, HttpCompletionOption.ResponseHeadersRead, tokenSource.Token );
                }

                // update function service metrics
                metrics.ProxyCompletedRequestsTotal( serviceNamespace, serviceName ).Inc();
            }
            catch ( TaskCanceledException )
            {
                response = null;
            }
            catch ( Exception ex )
            {
                isConnectionError = ( ex.InnerException is SocketException );
                response = null;
            }
        };

        await taskExecute();

        var maxAttempts = 5;
        var attempts = 0;

        /*
        retries only occur for connection errors (e.g. socket exception) or 503 errors (service unavailable)
        */
        while ( isConnectionError && ( response == null || response?.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ) )
        {
            if ( cancellationToken.IsCancellationRequested )
            {
                break;
            }

            attempts++;

            if ( attempts == maxAttempts )
            {
                response = null;
                break;
            }

            await Task.Delay( 1000 );

            await taskExecute();
        }

        if ( response == null )
        {
            throw new TimeoutException();
        }

        return ( response );
    }

    private HttpMethod GetMethod( HttpRequest source )
        => new HttpMethod( source.Method );
}
