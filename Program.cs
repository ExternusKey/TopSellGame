using HtmlAgilityPack;
using System.Diagnostics;

class Program
{
    static async Task Main()
    {
        string urlTopSellGame = "https://store.steampowered.com/search/?filter=topsellers&cc=ru";
        int countGames = 10;
        try
        {
            Console.Clear();
            HttpClient httpClient = new();
            using HttpClient client = httpClient;
            HttpResponseMessage response = await client.GetAsync(urlTopSellGame);
            response.EnsureSuccessStatusCode();
            string pageContent = await response.Content.ReadAsStringAsync();

            HtmlDocument doc = new();
            doc.LoadHtml(pageContent);

            var topSellNodes = doc.DocumentNode.SelectNodes("//div[@id='search_resultsRows']/a") ?? throw new ArgumentNullException("Не найдена таблица лидеров.");

            var topSellGameList = topSellNodes
                .Take(countGames)
                .Select((node, index) => new
                {
                    rank = index + 1,
                    gameName = node.SelectSingleNode(".//span[@class='title']")?.InnerText.Trim(),
                    gamePrice = node.SelectSingleNode(".//div[@class='discount_final_price']")?.InnerText.Trim() ?? "Бесплатно",
                    gameUrl = node.GetAttributeValue("href", null)
                });

            foreach (var gameInfo in topSellGameList)
            {
                if (gameInfo.gameName == null)
                    throw new ArgumentNullException("Значение названия не может быть null.");
                if (gameInfo.gameUrl == null)
                    throw new ArgumentNullException("Значение ссылки не может быть null.");
            }

            int nonEmptyTopSellGameListCount = topSellGameList
                .Where(node => !string.IsNullOrWhiteSpace(node.gameName))
                .Count();

            if (nonEmptyTopSellGameListCount < countGames)
                Console.WriteLine($"Количество игр на странице {nonEmptyTopSellGameListCount}, что меньше заявленного количества " +
                    $"({countGames}). Выводим Топ-{nonEmptyTopSellGameListCount}.");

            while (true)
            {
                Console.WriteLine($"Топ-{nonEmptyTopSellGameListCount} лидеров продаж в Steam (РФ):");

                foreach (var gameInfo in topSellGameList)
                    Console.WriteLine($"{gameInfo.rank,-2}. {gameInfo.gameName,-35} Стоимость - {gameInfo.gamePrice,12}");

                Console.WriteLine("Если хотите перейти на страницу игры в браузере, введите номер позиции:");
                Console.Write("Номер игры - ");
                bool choose = int.TryParse(Console.ReadLine(), out int number);

                if (number > 0 && number <= nonEmptyTopSellGameListCount)
                {
                    var game = topSellGameList.ElementAtOrDefault(number - 1);
                    string link = game.gameUrl;

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = link,
                        UseShellExecute = true
                    });

                    Console.Clear();
                    Console.WriteLine($"Переход на страницу игры {game.gameName} успешно осуществлён.");
                    break;
                }
                else
                {
                    Console.Clear();
                    HandleError($"Некорретный ввод: Введите целое число [1 - {nonEmptyTopSellGameListCount}].");
                }
            }
        }
        catch (HttpRequestException e)
        {
            HandleError("Ошибка при выполнении HTTP-запроса: ", e.Message);
        }
        catch (TaskCanceledException e)
        {
            HandleError("Истекло время ожидания запроса: ", e.Message);
        }
        catch (ArgumentNullException e)
        {
            HandleError("Ошибка в парсинге страницы: ", e.ParamName);
        }
    }

    static void HandleError(string typeError, string errorMessage = "")
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(typeError + errorMessage);
        Console.ResetColor();
    }
}