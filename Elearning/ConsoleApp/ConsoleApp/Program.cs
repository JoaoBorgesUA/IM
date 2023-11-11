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
using Microsoft.VisualBasic;
using OpenQA.Selenium.DevTools;
using System.ComponentModel;

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
    private static async Task SendMessage(ClientWebSocket client, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        Console.WriteLine($"Sent message: {message}");
    }

    static async Task ProcessMessages(ClientWebSocket client)
    {

        byte[] buffer = new byte[1024];

        ChromeOptions options = new ChromeOptions();
        options.AddArgument("2>&1");
        //IWebDriver driver = new ChromeDriver(@"C:\Users\35191\Downloads\chromedriver-win64\chromedriver-win64");
        IWebDriver driver = new ChromeDriver(options);
        bool website_open = false;
        bool authn_open = false;
        bool myaccount = false;

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
                    //Console.WriteLine("Received message: " + message);

                    var doc = XDocument.Parse(message);
                    var com = doc.Descendants("command").FirstOrDefault().Value;
                    dynamic messageJSON = JsonConvert.DeserializeObject(com);

                    //Console.WriteLine(messageJSON["nlu"] == null ? "Sim" : "Nao");

                    // Only process the message if there is something in the nlu parameter 
                    // To resolve the runtime error 
                    if (messageJSON["nlu"] != null)
                    {
                        Console.WriteLine(messageJSON["nlu"]);

                        string intent = (string)messageJSON["nlu"]["intent"];
                        if (intent == "open_elearning")
                        {
                            driver.Navigate().GoToUrl("https://elearning.ua.pt/");

                            website_open = true;
                            if (website_open)
                            {
                                string msg = messageMMI("Bem-vindo ao portal da universidade de aveiro");
                                await SendMessage(client, msg);
                            }
                        }

                        if (intent == "close_elearning")
                        {
                            if (website_open)
                            {
                                driver.Url = "about:blank";

                                string msg = messageMMI("Adeus! Até uma proxima.");
                                await SendMessage(client, msg);

                                website_open = false;
                            }
                            else {
                                string msg = messageMMI("O portal ainda não está aberto.");
                                await SendMessage(client, msg);
                            }
                        }

                        if (intent == "authenticate") 
                        {
                            if (website_open)
                            {
                                IWebElement login_button = driver.FindElement(By.ClassName("btn-login"));
                                login_button.Click();

                                authn_open = true;
                                
                                if (authn_open)
                                {
                                    string msg = messageMMI("Diga-me o seu número mecanográfico por favor");
                                    await SendMessage(client, msg);
                                }
                            }
                            else {
                                string msg = messageMMI("O website elearning ainda não está aberto.");
                                await SendMessage(client, msg);
                            }
                        }

                        if (intent == "insert_data_authn")
                        {
                            if (website_open && authn_open)
                            {
                                string student_nmec = messageJSON["nlu"]["studentnmec"];
                                Console.WriteLine(student_nmec); 

                                if (student_nmec.Length != 5)   // considering that the nmec has always five digits
                                {
                                    string msg = messageMMI("Por Favor repita o número do aluno !");
                                    await SendMessage(client, msg);
                                }
                                else
                                {
                                    if (student_nmec == "98678")
                                    {
                                        /*** Insert username and password + button authenticate **/
                                        IWebElement inputField = driver.FindElement(By.Id("username"));
                                        inputField.Clear(); // Clear existing text
                                        inputField.SendKeys(" ");

                                        IWebElement passwordField = driver.FindElement(By.Id("password"));
                                        passwordField.SendKeys(" ");

                                        IWebElement autenticar = driver.FindElement(By.Id("btnLogin"));
                                        autenticar.Click();

                                        string msg = messageMMI("Bem vinda Ana Rosa ! ");
                                        await SendMessage(client, msg);

                                        myaccount = true;
                                    }
                                    else
                                    {
                                        string msg = messageMMI("Os dados que pediu não se encontram na base de " +
                                                                "dados da universidade de aveiro");
                                        await SendMessage(client, msg);
                                    }
                                }
                            }
                        }

                        if (intent == "see_notifications")
                        {
                            if (myaccount)
                            {
                                IWebElement see_notifications = driver.FindElement(By.Id("nav-notification-popover-container"));
                                see_notifications.Click();
                            }
                        }

                        //if (intent == "logout")
                        //{
                        //    if (myaccount)
                        //    {
                        //        // 
                        //    }
                        //}
                    }
                }
            }
        }
    }

    public static string messageMMI(string msg)
    {
        return "<mmi:mmi xmlns:mmi=\"http://www.w3.org/2008/04/mmi-arch\" mmi:version=\"1.0\">" +
                    "<mmi:startRequest mmi:context=\"ctx-1\" mmi:requestId=\"text-1\" mmi:source=\"APPSPEECH\" mmi:target=\"IM\">" +
                        "<mmi:data>" +
                            "<emma:emma xmlns:emma=\"http://www.w3.org/2003/04/emma\" emma:version=\"1.0\">" +
                                "<emma:interpretation emma:confidence=\"1\" emma:id=\"text-\" emma:medium=\"text\" emma:mode=\"command\" emma:start=\"0\">" +
                                    "<command>\"&lt;speak version=\\\"1.0\\\" xmlns=\\\"http://www.w3.org/2001/10/synthesis\\\" xmlns:xsi=\\\"http://www.w3.org/2001/XMLSchema-instance\\\" xsi:schemaLocation=\\\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\\\" xml:lang=\\\"pt-PT\\\"&gt;&lt;p&gt;"+msg+"&lt;/p&gt;&lt;/speak&gt;\"</command>" +
                                "</emma:interpretation>" +
                                "</emma:emma>" +
                        "</mmi:data>" +
                    "</mmi:startRequest>" +
                "</mmi:mmi>";
    }
}
