﻿using HtmlAgilityPack;
using System.Diagnostics;

/*Получение данных было произведено через парсинг HTML-страницы
Пытался найти способ получения данных через Web API, но не нашёл метода, позволяющего получать лидеров продаж.
Встречал сторонние сервисы с похожими методами, но они не соответствовали требованиям задания (Пример - Вывод топа популярных игр по количеству игроков за 2 недели.)*/

class Program
{
    static async Task Main()
    {
        string urlTopSellGame = "https://store.steampowered.com/search/?filter=topsellers&cc=ru";
        int countGames = 10;
        string pageContent;

        try
        {
            using (HttpClient client = new())
            {
                HttpResponseMessage response = await client.GetAsync(urlTopSellGame);
                response.EnsureSuccessStatusCode();
                pageContent = await response.Content.ReadAsStringAsync();
            }
			HtmlDocument doc = new();
			doc.LoadHtml(pageContent);
			
			var topSellNodes = doc.DocumentNode.SelectNodes("//div[@id='search_resultsRows']/a") ??
				throw new ArgumentNullException("Не найдена таблица лидеров.");

			var topSellGameList = topSellNodes
				.Take(countGames)
				.Select((node, index) => new
				{
					rank = index + 1,
					gameName = node.SelectSingleNode(".//span[@class='title']")?.InnerText.Trim() ??
						throw new ArgumentNullException("Значение названия не может быть null."),

					gamePrice = node.SelectSingleNode(".//div[@class='discount_final_price']")?.InnerText.Trim() ??
						node.SelectSingleNode(".//div[@class='discount_final_price free']")?.InnerText.Trim().Replace("Free", "Бесплатно") ??
						throw new ArgumentNullException("Значение цены не может быть null."),

					gameUrl = node.GetAttributeValue("href", null) ??
						throw new ArgumentNullException("Значение ссылки не может быть null.")
				});

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

				if (int.TryParse(Console.ReadLine(), out int number) && number > 0 && number <= nonEmptyTopSellGameListCount)
				{
					var game = topSellGameList.ElementAt(number - 1);

					Process.Start(new ProcessStartInfo
					{
						FileName = game.gameUrl,
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