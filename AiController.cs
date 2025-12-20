using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjeOdeviWeb_G231210048.Data; // Veritabanı için gerekli
using ProjeOdeviWeb_G231210048.Models.ViewModels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    [Authorize]
    public class AiController : Controller
    {
        private readonly ApplicationDbContext _context; // 1. Veritabanı Değişkeni

        // 👇 SENİN API ANAHTARIN (Koddan aldım)
        private readonly string _apiKey = "AIzaSyCKv9oqW_UbN-uW66LX77Le2YQLDcrMvmU";

        // 2. Constructor: Veritabanını içeri alıyoruz
        public AiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new AiRequestViewModel();

            var userIdClaim = User.FindFirst("UserId");

            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);
                var user = _context.AppUsers.Find(userId);

                if (user != null)
                {
                    // DÜZELTME BURADA YAPILDI 👇
                    // (int) diyerek veriyi zorla tam sayıya çeviriyoruz.
                    // Böylece veritabanında "70.5" bile olsa buraya "70" olarak hatasız gelir.

                    model.Age = (int)(user.Age ?? 0);
                    model.Height = (int)(user.Height ?? 0);
                    model.Weight = (int)(user.Weight ?? 0);

                    // Cinsiyet string olduğu için hata vermez
                    model.Gender = user.Gender;

                    // Varsayılanlar
                    model.ActivityLevel = "Hareketsiz (Masa başı)";
                    model.Goal = "Formu Korumak";
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(AiRequestViewModel model)
        {
            if (_apiKey.Contains("BURAYA") || _apiKey.Length < 10)
            {
                ModelState.AddModelError("", "API Key girilmemiş.");
                return View(model);
            }

            string cleanKey = _apiKey.Trim();

            using (var httpClient = new HttpClient())
            {
                // 1. ADIM: Google'a "Benim kullanabileceğim modelleri listele" diyoruz.
                string listModelsUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={cleanKey}";

                try
                {
                    var listResponse = await httpClient.GetAsync(listModelsUrl);
                    var listResponseString = await listResponse.Content.ReadAsStringAsync();

                    if (!listResponse.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", $"BAĞLANTI HATASI: Anahtarınız hatalı olabilir. Google Cevabı: {listResponse.StatusCode} - {listResponseString}");
                        return View(model);
                    }

                    // 2. ADIM: Listeden çalışan ilk "generateContent" destekli modeli bulalım
                    var jsonNode = JsonNode.Parse(listResponseString);
                    var models = jsonNode?["models"]?.AsArray();

                    string validModelName = "";
                    string allAvailableModels = "";

                    if (models != null)
                    {
                        foreach (var m in models)
                        {
                            string name = m?["name"]?.ToString();
                            string methods = m?["supportedGenerationMethods"]?.ToString();
                            allAvailableModels += name + ", ";

                            if (methods != null && methods.Contains("generateContent") && name.Contains("gemini"))
                            {
                                validModelName = name;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(validModelName))
                    {
                        ModelState.AddModelError("", $"HATA: Uygun model bulunamadı. Erişiminiz olan modeller: {allAvailableModels}");
                        return View(model);
                    }

                    // 3. ADIM: İsteği Gönder
                    string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/{validModelName}:generateContent?key={cleanKey}";

                    string prompt = @$"Sen bir spor hocasısın. 
                                       Kullanıcı Bilgileri: {model.Age} yaş, {model.Gender}, {model.Weight}kg, {model.Height}cm.
                                       Aktivite: {model.ActivityLevel}, Hedef: {model.Goal}.
                                       
                                       Görevin:
                                       1. Günlük kalori ihtiyacını hesapla.
                                       2. 1 günlük örnek diyet listesi yaz.
                                       3. 3 günlük egzersiz programı yaz.
                                       
                                       ÖNEMLİ: Cevabı SADECE HTML formatında ver (div, b, ul, li, h4 kullan). Markdown kullanma.";

                    var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                    var jsonContent = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(requestUrl, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var resultNode = JsonNode.Parse(responseString);
                        string aiText = resultNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                        if (!string.IsNullOrEmpty(aiText))
                        {
                            model.AiResponse = aiText.Replace("```html", "").Replace("```", "");
                        }
                        else
                        {
                            model.AiResponse = "Yapay zeka boş cevap döndü.";
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Üretim Hatası ({validModelName}): {response.StatusCode} - {responseString}");
                    }

                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Kritik Hata: " + ex.Message);
                }
            }

            return View(model);
        }
    }
}
