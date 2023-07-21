using System.Collections.Concurrent;
using HHParser.HHClasses;
using Newtonsoft.Json;

const string baseName = "VacancyIds.json";
List<int> previousIds;
if (File.Exists(baseName))
{
    var oldJson = await File.ReadAllTextAsync(baseName);
    previousIds = JsonConvert.DeserializeObject<List<int>>(oldJson);
    if (previousIds == null)
        throw new Exception("Cannot parse previous ids");
}
else
{
    previousIds = new List<int>();
}

var newVacancies = (await GetAllVacancy()).Where(x => !previousIds.Contains(x.id)).ToList();
var addresses = newVacancies.Select(x => x.alternate_url);
Console.WriteLine(JsonConvert.SerializeObject(addresses));
previousIds.AddRange(newVacancies.Select(x => x.id));
var newJson = JsonConvert.SerializeObject(previousIds);
await File.WriteAllTextAsync(baseName, newJson);

async Task<IEnumerable<Vacancy>> GetAllVacancy()
{
    const int pagesCount = 20;
    var result = new ConcurrentBag<Vacancy>();
    await Parallel.ForEachAsync(Enumerable.Range(0, pagesCount),  async (page, ct) =>
    {
        using var client = new HttpClient(new HttpClientHandler());
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        client.BaseAddress = new Uri("https://api.hh.ru/");
        var response = await 
            client.GetAsync($"vacancies?text=C%23&per_page=100&employment=full&only_with_salary=true&page={page}&period=2", ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        var vacanciesRespond = JsonConvert.DeserializeObject<VacanciesRespond>(json);
        if (vacanciesRespond?.items == null)
            return;
        foreach (var vacancy in vacanciesRespond.items)
        {
            if (vacancy.salary.currency == "EUR" || vacancy.salary.currency == "USD")
            {
                result.Add(vacancy);
            }
        }
    });
    return result;
}
