using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace project_cat.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CatController : Controller
    {
        private IMemoryCache _cache;//переменная, которая будет использоваться для кэширования данных в памяти
        private static int _statusCode = 100;//дефолтный статус-код
        static readonly HttpClient Client = new HttpClient();//создание глобального экземпляра, для отправки HTTP-запросов и получения ответов

        public CatController(IMemoryCache memoryCache) 
        {
            _cache = memoryCache;//объект сохраняется в переменную, для дальнейшего использования для доступа к кэшу памяти
        }

        private async Task<byte[]> DownloadImageAsync(string url)
        {
            var data = await new HttpClient().GetAsync(url);//GetAsync отправляет асинхронный GET-запрос на указанный url-адрес и возвращает объект HttpResponseMessage,
                                                            //который содержит ответ от сервера. Результат запроса сохраняется в переменной data
            byte[] image = await data.Content.ReadAsByteArrayAsync();//массив байтов image, содержащий содержимое запроса
            return image;
        }

        private async Task<byte[]> CacheGetAsync(string url)//принимает url в кач-ве параметра и возвращает массив байтов
        {
            if (_cache.TryGetValue(url, out byte[] value))//проверка, есть ли значение для данного url в кэше
            {
                return value;//если есть, то возврат
            }

            CacheSetAsync(url, await DownloadImageAsync(url));//если нет, то это значение добавляется в кэш, а затем рекурсивно вызывается метод CacheGet для получения значения из кэша
                                                              //DownloadImage загружает изображение по заданному url и возвращает его как массив байтов
            return await CacheGetAsync(url);//возврат данных из кэша
        }

        private async Task CacheSetAsync(string url/*строка-ключ для кэширования*/, byte[] img/*массив байтов, который нужно закэшировать*/)
        {
            await Task.Run(() => _cache.Set(url, img, TimeSpan.FromSeconds(10)));//метод set объекта _cache записывает в кэш данные по указанному ключу url и задает время жизни кэша в 10 секунд
        }

        private async Task<byte[]> GetDefaultAsync(string path)//асинхронный метод, для того, чтобы прочитать все байты из файла, указанного в path и затем вернуть массив байтов, который содержит файл
        {
            return await System.IO.File.ReadAllBytesAsync(path);
        }

        [HttpGet]//метод действия веб-контроллера, который обрабатывает GET-запросы к url-адресу ProcessUrl
        [Route("ProcessUrl")]
        public async Task<FileContentResult> ProcessUrlAsync(string url)//метод, который принимает url-адрес в качестве параметра и возвращает содержимое файла в виде объекта FileContentResult.
        {

            static bool IsValidUrl(string url)//проверка, является ли заданный url корректным и является ли этот адрес HTTP или HTTPS
            {
                Uri? uriResult;
                return Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                        (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }

            if (!IsValidUrl(url))//если заданный url не является корректным, то выводим картинку с ошибкой
            {
                return File(await GetDefaultAsync("./images/error.jpg"/*путь к файлу*/), "image/jpeg"/*тип содержимого файла*/);
            }

            try
            {
                HttpResponseMessage response = await Client.GetAsync(url);//выполнение GET-запрос к указанному url-адресу
                _statusCode = Convert.ToInt32(response.StatusCode);//если запрос успешен, код состояния ответа сохраняется в переменной _statusCode
            }
            catch
            {
                return File(await GetDefaultAsync("./images/404.jpg"/*путь к файлу*/), "image/jpeg"/*тип содержимого файла*/);//если возникает исключение, возвращаем картинку с ошибкой
            }

            return File(await CacheGetAsync($"https://http.cat/{_statusCode}.jpg"/*путь к файлу*/), "image/jpeg"/*тип содержимого файла*/);//если все хорошо, выводим соответствующую статус-коду картинку с сайта 
        }
    }
}