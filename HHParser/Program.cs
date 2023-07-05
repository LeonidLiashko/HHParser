using System.Collections.Concurrent;
using HHParser.HHClasses;
using Newtonsoft.Json;

var addresses = GetAllVacancy().Select(x => x.alternate_url);
Console.WriteLine(JsonConvert.SerializeObject(addresses));

IEnumerable<Vacancy> GetAllVacancy()
{
    const int pagesCount = 20;
    var result = new ConcurrentBag<Vacancy>();
    Parallel.For(0, pagesCount,  page =>
    {
        using var client = new HttpClient(new HttpClientHandler());
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        client.BaseAddress = new Uri("https://api.hh.ru/");
        var response =
            client.GetAsync($"vacancies?text=C%23&per_page=100&employment=full&only_with_salary=true&page={page}&period=2").Result;
        var json = response.Content.ReadAsStringAsync().Result;
        var vacanciesRespond = JsonConvert.DeserializeObject<VacanciesRespond>(json);
        if (vacanciesRespond?.items == null)
            return;
        foreach (var item in vacanciesRespond.items)
        {
            if (item.salary.currency == "EUR" || item.salary.currency == "USD")
            {
                result.Add(item);
            }
        }
    });
    return result;
}
