using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string host = "127.0.0.1"; // Replace with your actual host
        string path = "/IM/USER1/APP"; // Replace with your WebSocket path

        using (ClientWebSocket client = new ClientWebSocket())
        {
            Uri uri = new Uri("wss://" + host + ":8005" + path);

            try
            {
                await client.ConnectAsync(uri, CancellationToken.None);

                Console.WriteLine("Connected to the WebSocket server.");

                // Handle messages and other logic here
                await ReceiveMessages(client);

                // Close the WebSocket when done
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error: {ex.Message}");
            }
        }
    }

    static async Task ReceiveMessages(ClientWebSocket client)
    {
        byte[] buffer = new byte[1024];

        while (client.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received message: " + message);

                //using (IWebDriver driver = new ChromeDriver())
                //{
                //    // Navigate to a webpage
                //    driver.Navigate().GoToUrl("https://example.com");

                //    // You can perform various actions on the webpage, for example, finding an element by its ID and interacting with it
                //    IWebElement element = driver.FindElement(By.Id("elementId"));
                //    element.SendKeys("Hello, Selenium!");

                //    // Close the WebDriver
                //    driver.Quit();
                //}

                //Console.ReadLine(); // Keep the application running
            }
        }
    }

    // Define your im1MessageHandler function here for processing WebSocket messages
    // static void im1MessageHandler(string data) { }
}
