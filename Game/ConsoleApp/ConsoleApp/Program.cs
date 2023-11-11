using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    static async Task Main(string[] args)
    {
        string host = "127.0.0.1"; // Replace with your actual host
        string path = "/IM/USER1/APP"; // Replace with your WebSocket path

        // Receive Messages from rasa 
        using (ClientWebSocket client = new ClientWebSocket())
        {
            Uri uri = new Uri("wss://" + host + ":8005" + path);

            try
            {
                await client.ConnectAsync(uri, CancellationToken.None);

                Console.WriteLine("Connected to the WebSocket server.");

                // Handle messages and other logic here
                await ProcessMessages(client);

                // Close the WebSocket when done
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error: {ex.Message}");
            }
        }
    }

    static async Task ProcessMessages(ClientWebSocket client)
    {
        byte[] buffer = new byte[1024];

        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--ignore-certificate-errors");
        IWebDriver driver = new ChromeDriver(@"chromedriver.exe");
        //IWebDriver driver = new ChromeDriver();
        bool gameOpen = false;
        bool gameStart = false;

        while (client.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (message == "OK")
                {
                    Console.WriteLine("Received message OK: " + message);
                }
                else if (message != null && message!="RENEW")
                {
                    Console.WriteLine("Received message: " + message);

                    var doc = XDocument.Parse(message);
                    var com = doc.Descendants("command").FirstOrDefault().Value;
                    dynamic messageJSON = JsonConvert.DeserializeObject(com);

                    //Console.WriteLine(messageJSON["nlu"] == null ? "Sim" : "Nao");

                    // Only process the message if there is something in the nlu parameter 
                    // To resolve the runtime error 
                    if (messageJSON["nlu"] != null)
                    {
                        string intent = (string)messageJSON["nlu"]["intent"];

                        if (intent == "open_game")
                        {
                            driver.Navigate().GoToUrl("https://www.shellshock.io");   //Open a URL

                            // Find the "Accept Cookies" button by its class name
                            IWebElement acceptCookiesButton = driver.FindElement(By.ClassName("cmpboxbtn"));
                            // Click the "Accept Cookies" button
                            acceptCookiesButton.Click();

                            string currentURL = driver.Url.ToString();
                            if (currentURL == "https://shellshock.io/")
                            {
                                gameOpen = true;

                                Uri serverUri = new Uri("ws://127.0.0.1:8000/IM/USER1/APPSPEECH"); // Replace with your WebSocket server URL

                                using (ClientWebSocket mmiClient = new ClientWebSocket())
                                {
                                    await mmiClient.ConnectAsync(serverUri, CancellationToken.None);

                                    // The message you want to send
                                    string text = "Jogo Aberto";

                                    string speak = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\" xml:lang=\"pt-PT\"><p>"+text+"</p></speak>";
                                    //var result = speak;
                                    //mmiCli_1.sendToIM(new LifeCycleEvent("APPSPEECH", "IM", "text-1", "ctx-1").
                                    //    doStartRequest(new EMMA("text-", "text", "command", 1, 0).fk
                                    //      setValue(JSON.stringify(result))));

                                    // Convert the message to bytes
                                    byte[] messageBytes = Encoding.UTF8.GetBytes(speak);

                                    // Send the message
                                    await mmiClient.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                        }

                        if (intent == "close_game")
                        {
                            if (gameOpen)
                            {
                                driver.Close(); // close tab
                                driver.Quit();  // Close the browser
                            }
                            else
                            {
                                // fazer com q o sistema fale connosco a dizer que o jogo ainda nao esta aberto
                            }
                        }

                        if (intent == "start_game") // Ir para a arena do jogo
                        {
                            if (gameOpen)
                            {
                                IWebElement playButton = driver.FindElement(By.ClassName("play-button"));
                                playButton.Click();

                                gameStart = true;
                            }
                            else
                            {
                                // fazer com q o sistema fale connosco a dizer que o jogo ainda nao esta aberto
                            }
                        }

                        // nao esta a dar , nao sei que nome usar no id ou classname!!! 
                        if (intent == "close_tutorial_popup")
                        {
                            if (gameOpen)
                            {
                                IWebElement closePopup = driver.FindElement(By.ClassName("clickme"));
                                closePopup.Click();
                            }
                            else
                            {
                                // fazer com q o sistema fale connosco a dizer que o jogo ainda nao esta aberto
                            }
                        }

                        /*******    Teclas / Funcionalidades:
                         *  W - Subir
                         *  A - Mover para a esquerda 
                         *  S - Mover para baixo
                         *  D - Mover para a direira
                         *  Q - Granada 
                         *  E - Trocar arma
                         *  R - Recarregar
                         *  F - Corpo a Corpo
                         *  SHIFT - Mirar 
                         *  SPACEBAR - Pular 
                         *  CLICK - Disparar 
                         */

                        if (intent == "forward")    // Tecla 'w' - Subir/Andar pra frente
                        {
                            if (gameOpen && gameStart)  // Se jogo aberto e tivermos na arena
                            {
                                //Actions action = new Actions(driver);

                                // Send the 'W' key to move the character forward
                                //while (true)
                                //{
                                //    action.SendKeys(OpenQA.Selenium.Keys.Control + "W").Build().Perform();
                                //}
                            }
                        }

                        //action.SendKeys(OpenQA.Selenium.Keys.Escape).Build().Perform(); // sair da tela total

                        if (intent == "jump")
                        {
                            if (gameOpen && gameStart)  // Se jogo aberto e tivermos na arena
                            {
                                Actions action = new Actions(driver);
                                action.SendKeys(OpenQA.Selenium.Keys.Escape).Build().Perform();

                                // element.SendKeys(Keys.Control + "a");
                            }
                        }

                        if (intent == "shoot")  
                        {
                            if (gameOpen && gameStart)  // Se jogo aberto e tivermos na arena
                            {
                                IWebElement game_canvas = driver.FindElement(By.Id("canvas"));
                                game_canvas.Click();
                            }
                        }
                    }
                }
            }
        }
    }

    //IWebElement gotoPerfil = driver.FindElement(By.ClassName("btn-account-status"));
    //IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
    //js.ExecuteScript("arguments[0].scrollIntoView();", gotoPerfil);
    //                    playButton.Click();

    //                    // Click the "Accept Cookies" button
    //                    gotoPerfil.Click();

    //using (IWebDriver driver = new ChromeDriver())
    //{
    //    // Navigate to a webpage
    //    driver.Navigate().GoToUrl("https://www.shellshock.io");

    //    // You can perform various actions on the webpage, for example, finding an element by its ID and interacting with it
    //    //IWebElement element = driver.FindElement(By.Id("elementId"));
    //    //element.SendKeys("Hello, Selenium!");

    //    // Close the WebDriver
    //    //driver.Quit();
    //}

    //Console.ReadLine(); // Keep the application running

    // Define your im1MessageHandler function here for processing WebSocket messages
    // static void im1MessageHandler(string data) { }
}
