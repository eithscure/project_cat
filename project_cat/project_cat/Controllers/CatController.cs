using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace project_cat.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CatController : Controller
    {
        private IMemoryCache _cache;//����������, ������� ����� �������������� ��� ����������� ������ � ������
        private static int _statusCode = 100;//��������� ������-���
        static readonly HttpClient Client = new HttpClient();//�������� ����������� ����������, ��� �������� HTTP-�������� � ��������� �������

        public CatController(IMemoryCache memoryCache) 
        {
            _cache = memoryCache;//������ ����������� � ����������, ��� ����������� ������������� ��� ������� � ���� ������
        }

        private async Task<byte[]> DownloadImageAsync(string url)
        {
            var data = await new HttpClient().GetAsync(url);//GetAsync ���������� ����������� GET-������ �� ��������� url-����� � ���������� ������ HttpResponseMessage,
                                                            //������� �������� ����� �� �������. ��������� ������� ����������� � ���������� data
            byte[] image = await data.Content.ReadAsByteArrayAsync();//������ ������ image, ���������� ���������� �������
            return image;
        }

        private async Task<byte[]> CacheGetAsync(string url)//��������� url � ���-�� ��������� � ���������� ������ ������
        {
            if (_cache.TryGetValue(url, out byte[] value))//��������, ���� �� �������� ��� ������� url � ����
            {
                return value;//���� ����, �� �������
            }

            CacheSetAsync(url, await DownloadImageAsync(url));//���� ���, �� ��� �������� ����������� � ���, � ����� ���������� ���������� ����� CacheGet ��� ��������� �������� �� ����
                                                              //DownloadImage ��������� ����������� �� ��������� url � ���������� ��� ��� ������ ������
            return await CacheGetAsync(url);//������� ������ �� ����
        }

        private async Task CacheSetAsync(string url/*������-���� ��� �����������*/, byte[] img/*������ ������, ������� ����� ������������*/)
        {
            await Task.Run(() => _cache.Set(url, img, TimeSpan.FromSeconds(10)));//����� set ������� _cache ���������� � ��� ������ �� ���������� ����� url � ������ ����� ����� ���� � 10 ������
        }

        private async Task<byte[]> GetDefaultAsync(string path)//����������� �����, ��� ����, ����� ��������� ��� ����� �� �����, ���������� � path � ����� ������� ������ ������, ������� �������� ����
        {
            return await System.IO.File.ReadAllBytesAsync(path);
        }

        [HttpGet]//����� �������� ���-�����������, ������� ������������ GET-������� � url-������ ProcessUrl
        [Route("ProcessUrl")]
        public async Task<FileContentResult> ProcessUrlAsync(string url)//�����, ������� ��������� url-����� � �������� ��������� � ���������� ���������� ����� � ���� ������� FileContentResult.
        {

            static bool IsValidUrl(string url)//��������, �������� �� �������� url ���������� � �������� �� ���� ����� HTTP ��� HTTPS
            {
                Uri? uriResult;
                return Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                        (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }

            if (!IsValidUrl(url))//���� �������� url �� �������� ����������, �� ������� �������� � �������
            {
                return File(await GetDefaultAsync("./images/error.jpg"/*���� � �����*/), "image/jpeg"/*��� ����������� �����*/);
            }

            try
            {
                HttpResponseMessage response = await Client.GetAsync(url);//���������� GET-������ � ���������� url-������
                _statusCode = Convert.ToInt32(response.StatusCode);//���� ������ �������, ��� ��������� ������ ����������� � ���������� _statusCode
            }
            catch
            {
                return File(await GetDefaultAsync("./images/404.jpg"/*���� � �����*/), "image/jpeg"/*��� ����������� �����*/);//���� ��������� ����������, ���������� �������� � �������
            }

            return File(await CacheGetAsync($"https://http.cat/{_statusCode}.jpg"/*���� � �����*/), "image/jpeg"/*��� ����������� �����*/);//���� ��� ������, ������� ��������������� ������-���� �������� � ����� 
        }
    }
}