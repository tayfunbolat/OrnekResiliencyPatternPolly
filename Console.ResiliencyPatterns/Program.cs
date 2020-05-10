using Polly;
using Polly.Timeout;
using RestSharp;
using Serilog;
using System;

namespace ResiliencyPatterns
{
    public class Program
    {
        static void Main(string[] args)
        {

            var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
             .WriteTo.RollingFile(@"C:\ResiliencyPatterns\log.txt", retainedFileCountLimit: 7)
            .CreateLogger();


            logger.Information("Console Çalıştı");

            RestClient restClient = new RestClient("https://localhost:44372");

            RestRequest restRequest = new RestRequest("/WeatherForecast/GETAPI1", Method.GET);

            #region Retry Policy

            //Api'ye yapılan istek toplamda 3 defa olacak.Her başarısız denemeden sonra 2*(retryAttempt) 3 defa denememizde Başarılı bir response alamazsak Başarsız dönüşü kabul etmiş oluyoruz.
            //var result = Policy.HandleResult<IRestResponse>(x => x.StatusCode != System.Net.HttpStatusCode.OK)
            //    .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),(response,timeSpan,retryCount,context) => 
            //    {

            //        logger.Warning($"{response.Result.StatusCode} aldın.{retryCount} Denemen başarısız {timeSpan} bekle ");

            //    })
            //    .Execute(() => restClient.Execute(restRequest));

            //WaitAndRetry çalıştı ve başarılı :)
            //if(result.StatusCode == System.Net.HttpStatusCode.OK)
            //    logger.Information($"Servis çalıştırıldı {result.StatusCode}");


            //Tüm denemelere rağmen başarısız :(
            //else
            //    logger.Warning($"Tüm denemelere rağmen servis başarısız");

            #endregion

            #region Circuit Breaker

            /* 3 defa hatayı tolere edebileceğimizi ve 3 defa yaptığımız istek sonucunda 
             response'u(200) alamazsak Circuit Breaker'ı açarak belirtilen süre boyunca
            bekletiyoruz. */
            //var circuitBreaker = Policy.HandleResult<IRestResponse>(x => x.StatusCode != System.Net.HttpStatusCode.OK)
            //   .CircuitBreaker(3, TimeSpan.FromSeconds(60), (result, timespan) =>
            //    {
            //        logger.Warning($"3 denemede başarısız.{result.Result.StatusCode} hatası alındı.Servis çalıştırılamadı {timespan} süresi boyunca isteğe kapandı");
            //    }, () =>
            //    {
            //        logger.Information("Devre kapalı, talepler normal şekilde akıyor");
            //    }, () =>
            //    {
            //        logger.Information("HalfOpen modunda devre, bir isteğe izin verilecektir.");
            //    });


            /////circuitBreaker.CircuitState bize state durumu hakkında bilgi veriyor.
            //logger.Information($"circuitBreaker durumumuz :{circuitBreaker.CircuitState}");

            //var response = circuitBreaker.Execute(() => restClient.Execute(restRequest));

            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //    logger.Information($"Servis çalıştırıldı {response.StatusCode}--circuitBreaker durumumuz :{circuitBreaker.CircuitState}");

            //else
            //    logger.Error($"Servis çalıştırılamadı. {response.StatusCode}--circuitBreaker durumumuz :{circuitBreaker.CircuitState}");


            #endregion


            #region Fallback
            //RestClient restClient = new RestClient("https://localhost:44372");

            //RestRequest restRequest = new RestRequest("/WeatherForecast/GETAPI1", Method.GET);

            ////Alternatif olarak farklı bir API endpoint'ine istek oluşturuyoruz.
            //RestClient restClient2 = new RestClient("https://localhost:44398");

            //RestRequest restRequest2 = new RestRequest("/WeatherForecast/GETAPI2", Method.GET);

            //var result = Policy.HandleResult<IRestResponse>(x => x.StatusCode != System.Net.HttpStatusCode.OK)
            //   .Fallback<IRestResponse>(() => restClient2.Execute(restRequest2), (response) =>
            //    {

            //        logger.Information($" {response.Result.ResponseUri} Servis çalışmadı {response.Result.StatusCode}");

            //    }).Execute(() => restClient.Execute(restRequest));


            //if (result.StatusCode == System.Net.HttpStatusCode.OK)
            //    logger.Information($" {result.ResponseUri} Servis çalıştı {result.StatusCode}");
            //else
            //    logger.Information($"{result.ResponseUri}  Servis çalışmadı {result.StatusCode}");

            #endregion


            #region TimeOut


            try
            {

                //TimeOut süresi olarak 15 saniye belirliyoruz. 15 Saniye içinde başarılı veya başarısız alınmayacak bir feedback,Timeout'a düşürecek.
                var result = Policy.Timeout(TimeSpan.FromSeconds(15), Polly.Timeout.TimeoutStrategy.Pessimistic, onTimeout: (context, timespan, task) =>
                {
                    logger.Warning($"{timespan.TotalSeconds} saniye sonra zaman aşımına uğradı..");

                }).Execute(() => restClient.Execute(restRequest));

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    logger.Information($"servis başarılı bir şekilde çalıştı");
                else
                    logger.Error($"servis çağrısı başarısız");
            }
            catch (TimeoutRejectedException ex)
            {

                logger.Error($"{ex.Message} TimeOut süresi doldu.Servis çağrısı iptal edildi.");
            }
           



            #endregion
        }

    }
}
