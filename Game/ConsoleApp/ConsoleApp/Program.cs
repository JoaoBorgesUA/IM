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
using WindowsInput;
using WindowsInput.Native;
using System.ComponentModel;
using OpenQA.Selenium.DevTools.V117.PerformanceTimeline;

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
        options.AddArgument("start-maximized");
        options.AddArgument("disable-infobars");
        options.AddArgument("--disable-extensions");
        IWebDriver driver = new ChromeDriver(@"chromedriver.exe", options);
        //IWebDriver driver = new ChromeDriver();
        bool gameOpen = false;
        bool gameStart = false;
        bool aiming = false;
        VirtualKeyCode lastKeyUsed = VirtualKeyCode.SPACE;

        while (client.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            InputSimulator sim = new InputSimulator();
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
                        string amount = (string)messageJSON["nlu"]["amount"];
                        string direction = (string)messageJSON["nlu"]["direction"];
                        string distance = (string)messageJSON["nlu"]["distance"];

                        if (intent == "open_game")
                        {

                            driver.Navigate().GoToUrl("https://www.shellshock.io");   //Open a URL

                            string currentURL = driver.Url.ToString();
                            gameOpen = true;
                            try {
                                // Find the "Accept Cookies" button by its class name
                                IWebElement acceptCookiesButton = driver.FindElement(By.ClassName("cmpboxbtn"));
                                // Click the "Accept Cookies" button
                                acceptCookiesButton.Click();

                            }
                            catch (Exception ex) {
                                Console.WriteLine(ex.ToString());
                            }
                            //if (currentURL == "https://shellshock.io/")
                            //{
                            //    gameOpen = true;

                            //    Uri serverUri = new Uri("ws://127.0.0.1:8000/IM/USER1/APPSPEECH"); // Replace with your WebSocket server URL

                            //    using (ClientWebSocket mmiClient = new ClientWebSocket())
                            //    {
                            //        await mmiClient.ConnectAsync(serverUri, CancellationToken.None);

                            //        // The message you want to send
                            //        string text = "Jogo Aberto";

                            //        string speak = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\" xml:lang=\"pt-PT\"><p>"+text+"</p></speak>";
                            //        //var result = speak;
                            //        //mmiCli_1.sendToIM(new LifeCycleEvent("APPSPEECH", "IM", "text-1", "ctx-1").
                            //        //    doStartRequest(new EMMA("text-", "text", "command", 1, 0).fk
                            //        //      setValue(JSON.stringify(result))));

                            //        // Convert the message to bytes
                            //        byte[] messageBytes = Encoding.UTF8.GetBytes(speak);

                            //        // Send the message
                            //        await mmiClient.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                            //    }
                            //}
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
                                try
                                {
                                    IWebElement playButton = driver.FindElement(By.ClassName("play-button"));
                                    playButton.Click();

                                    gameStart = true;
                                }
                                catch (Exception ex) {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            else
                            {
                                continue;
                                // fazer com q o sistema fale connosco a dizer que o jogo ainda nao esta aberto
                            }
                            if (gameStart)
                            {
                                try
                                {
                                    IWebElement closePopup = driver.FindElement(By.CssSelector("button[class='roundme_sm popup_close clickme']"));
                                    closePopup.Click();
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                        }

                        // nao esta a dar , nao sei que nome usar no id ou classname!!! 
                        if (intent == "close_tutorial_popup")
                        {
                            if (gameOpen)
                            {

                                IWebElement closePopup = driver.FindElement(By.ClassName("roundme_sm popup_close clickme"));
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
                                try
                                {
                                    sim.Keyboard.KeyDown(VirtualKeyCode.VK_W);
                                    lastKeyUsed = VirtualKeyCode.VK_W;
                                }
                            catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                        }

                        if (intent=="move")
                        {
                            try
                            {
                                switch (direction)
                                {
                                    case "esquerda":
                                        sim.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                                        lastKeyUsed = VirtualKeyCode.VK_A;
                                        break;
                                    case "direita":
                                        sim.Keyboard.KeyDown(VirtualKeyCode.VK_D);
                                        lastKeyUsed = VirtualKeyCode.VK_D;
                                        break;
                                    case "trás":
                                        sim.Keyboard.KeyDown(VirtualKeyCode.VK_S);
                                        lastKeyUsed = VirtualKeyCode.VK_S;
                                        break;
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }

                        if (intent == "stop")
                        {
                            try
                            {
                                sim.Keyboard.KeyUp(lastKeyUsed);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }

                        //action.SendKeys(OpenQA.Selenium.Keys.Escape).Build().Perform(); // sair da tela total

                        if (intent == "jump")
                        {
                                try{
                                sim.Keyboard.KeyDown(VirtualKeyCode.SPACE);
                                Thread.Sleep(50);
                                sim.Keyboard.KeyUp(VirtualKeyCode.SPACE);
                                }
                                catch (Exception ex){
                                    Console.WriteLine(ex.ToString());
                                }
                        }

                        if (intent == "shoot")  
                        {
                            try
                            {
                                if (amount != null)
                                {
                                    if (amount == "muito")
                                    {
                                        sim.Mouse.LeftButtonDown();
                                        Thread.Sleep(800);
                                        sim.Mouse.LeftButtonUp();
                                    }
                                    else
                                    {
                                        sim.Mouse.LeftButtonDown();
                                        Thread.Sleep(400);
                                        sim.Mouse.LeftButtonUp();
                                    }
                                    amount= null;
                                }
                                else
                                {
                                    sim.Mouse.LeftButtonClick();
                                }


                                if (aiming == true){
                                    sim.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
                                    aiming = false;
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }

                        if (intent == "look")    // Tecla 'w' - Subir/Andar pra frente
                        {
                            try
                            {
                                switch (direction)
                                {
                                    case "esquerda":
                                        sim.Mouse.MoveMouseBy(-180, 0);
                                        break;
                                    case "direita":
                                        sim.Mouse.MoveMouseBy(180, 0);
                                        break;
                                    case "cima":
                                        sim.Mouse.MoveMouseBy(0, 180);
                                        break;
                                    case "baixo":
                                        sim.Mouse.MoveMouseBy(0, -180);
                                        break;
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }

                        if (intent == "change_weapon")
                        {
                            try
                            {
                                sim.Keyboard.KeyDown(VirtualKeyCode.VK_E);
                                Thread.Sleep(50);
                                sim.Keyboard.KeyUp(VirtualKeyCode.VK_E);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                        if (intent == "reload")
                        {
                            try
                            {
                                sim.Keyboard.KeyDown(VirtualKeyCode.VK_R);
                                Thread.Sleep(50);
                                sim.Keyboard.KeyUp(VirtualKeyCode.VK_R);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                        if (intent == "melee")
                        {
                            try
                            {
                                sim.Keyboard.KeyDown(VirtualKeyCode.VK_F);
                                Thread.Sleep(50);
                                sim.Keyboard.KeyUp(VirtualKeyCode.VK_F);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                        if (intent == "aim")
                        {
                            try
                            {
                                sim.Keyboard.KeyDown(VirtualKeyCode.SHIFT);
                                aiming = !aiming;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                        if (intent == "shoot")
                        {
                            try
                            {
                                if (distance != null)
                                {
                                    if (distance == "alto" || distance == "longe")
                                    {
                                        sim.Keyboard.KeyDown(VirtualKeyCode.VK_Q);
                                        Thread.Sleep(800);
                                        sim.Keyboard.KeyDown(VirtualKeyCode.VK_Q);
                                    }
                                    else
                                    {
                                        sim.Keyboard.KeyDown(VirtualKeyCode.VK_Q);
                                        Thread.Sleep(300);
                                        sim.Keyboard.KeyDown(VirtualKeyCode.VK_Q);
                                    }
                                    distance = null;
                                }
                                else
                                {
                                    sim.Keyboard.KeyDown(VirtualKeyCode.VK_Q);
                                    Thread.Sleep(50);
                                    sim.Keyboard.KeyDown(VirtualKeyCode.VK_Q);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
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
