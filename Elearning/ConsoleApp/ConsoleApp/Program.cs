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
using System.Drawing;

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
        options.AddArgument("2>&1");    // skip warnings 
        //IWebDriver driver = new ChromeDriver(@"C:\Users\35191\Downloads\chromedriver-win64\chromedriver-win64");
        IWebDriver driver = new ChromeDriver(options);

        bool website_open = false;  // check if website is already opened
        bool authn_open = false;    // check if we are in the authentication page
        bool myaccount = false;     // check if we already are on our account
        bool events_open = false;   // check if we are in the events page (inside our account)
        bool popup_newevent = false;    // check if we can say the values for a new event

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

                    Console.WriteLine(messageJSON);

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
                                await SendMessage(client, messageMMI("Bem-vindo ao portal da universidade de aveiro"));
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
                                await SendMessage(client, messageMMI("O portal ainda não está aberto."));
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
                                //Console.WriteLine(student_nmec); 

                                if (student_nmec.Length != 5)   // considering that the nmec has always five digits
                                {
                                    string msg = messageMMI("Por Favor repita o número do aluno !");
                                    await SendMessage(client, msg);
                                }
                                else
                                {
                                    if (student_nmec == "98678")
                                    {
                                        string user = "";
                                        string password = "";

                                        if (user == "" || password == "")   // just to debug 
                                        {
                                            await SendMessage(client, messageMMI("A palavra-passe fornecida está incorreta."));
                                        }
                                        else
                                        {
                                            /*** Insert username and password + button authenticate **/
                                            IWebElement inputField = driver.FindElement(By.Id("username"));
                                            inputField.Clear();
                                            inputField.SendKeys(user);

                                            IWebElement passwordField = driver.FindElement(By.Id("password"));
                                            passwordField.SendKeys(password);

                                            IWebElement valueInputField = driver.FindElement(By.CssSelector("input[type='text']"));
                                            string inputValue = valueInputField.GetAttribute("value");

                                            IWebElement valuePassField = driver.FindElement(By.CssSelector("input[type='password']"));
                                            string passValue = valuePassField.GetAttribute("value");

                                            if (inputValue == user && passValue == password)    // make sure the keys have been sent 
                                            {
                                                IWebElement autenticar = driver.FindElement(By.Id("btnLogin"));
                                                autenticar.Click();

                                                myaccount = true;   // inside my account 
                                            }
                                        }

                                        if (myaccount)
                                        {
                                            await SendMessage(client, messageMMI("Bem vinda Ana Rosa ! "));
                                            authn_open = false; // outside the authentication page
                                        }
                                    }
                                    else
                                    {
                                        await SendMessage(client, messageMMI("Utilizador desconhecido"));
                                    }
                                }
                            }
                        }

                        if (intent == "see_notifications")  // FALTA INTENT PARA FECHAR 
                        {
                            if (myaccount)
                            {
                                IWebElement see_notifications = driver.FindElement(By.Id("nav-notification-popover-container"));
                                see_notifications.Click();
                            }
                        }

                        if (intent == "see_events") // See my events  -- Go to Events section
                        {
                            if (myaccount)
                            {
                                IWebElement see_events = driver.FindElement(By.CssSelector("a[title='Eventos']")); 
                                see_events.Click();

                                events_open = true;
                            }
                        }

                        if(intent == "schedule_event")  // create a new event 
                        {
                            if (myaccount & events_open)    
                            {
                                IWebElement new_event_Bttn = driver.FindElement(By.CssSelector("button[data-action='new-event-button']"));
                                new_event_Bttn.Click();

                                popup_newevent = true;

                                if (popup_newevent)
                                {
                                    await SendMessage(client, messageMMI("Por favor, Primeiro diga o nome e a data do evento !"));
                                }
                            }
                            else if(myaccount & !events_open)  // if we want to create a new event -- go directly to new event popup
                            {
                                IWebElement see_events = driver.FindElement(By.CssSelector("a[title='Eventos']"));  // go to events section
                                see_events.Click();
                                events_open = true;

                                if (events_open)
                                {
                                    IWebElement new_event_Bttn = driver.FindElement(By.CssSelector("button[data-action='new-event-button']"));
                                    new_event_Bttn.Click();

                                    popup_newevent = true;

                                    if (popup_newevent)
                                    {
                                        await SendMessage(client, messageMMI("Por favor, Primeiro diga o nome e a data do evento !"));
                                    }
                                }
                            }
                            else  // we aren't in our account
                            {
                                await SendMessage(client, messageMMI("Ainda não está autenticado !"));
                            }
                        }

                        if (intent == "name_date_newevent") // insert data values new event 
                        {
                            //if (myaccount & events_open & popup_newevent)
                            if (myaccount & events_open)
                            {
                                string name = messageJSON["nlu"]["eventname"];
                                string day = messageJSON["nlu"]["eventday"];
                                string month = messageJSON["nlu"]["eventmonth"];
                                string year = messageJSON["nlu"]["eventyear"];

                                Console.WriteLine("name: " + name + " day: " + day + " month: " + month + " year: " + year);

                                if (name == "" || day == "" || month == "" || year == "")
                                {
                                    await SendMessage(client, messageMMI("Repita Por favor !"));
                                }
                                else
                                {
                                    // Send event name 
                                    IWebElement input_event_name = driver.FindElement(By.Id("id_name"));
                                    input_event_name.Clear();
                                    input_event_name.SendKeys(name);

                                    //// Send event day 
                                    IWebElement select_event_day = driver.FindElement(By.Id("id_timestart_day"));
                                    SelectElement select_day = new SelectElement(select_event_day);
                                    select_day.SelectByValue(day);

                                    //// Send event month 
                                    IWebElement select_event_month = driver.FindElement(By.Id("id_timestart_month"));
                                    SelectElement select_month = new SelectElement(select_event_month);
                                    select_month.SelectByValue(month);

                                    //// Send event year 
                                    IWebElement select_event_year = driver.FindElement(By.Id("id_timestart_year"));
                                    SelectElement select_year = new SelectElement(select_event_year);
                                    select_year.SelectByValue(year);

                                    await SendMessage(client, messageMMI("Por favor, agora diga a hora do evento!"));
                                }

                                
                            }
                        }

                        if (intent == "time_newevent")
                        {
                            //if (myaccount & events_open & popup_newevent)
                            if (myaccount & events_open)
                            {
                                string hour = messageJSON["nlu"]["eventhour"];
                                string minutes = messageJSON["nlu"]["eventminutes"];

                                if (hour == "")
                                {
                                    await SendMessage(client, messageMMI("Não entendi. Repita a hora por favor"));
                                }else if (minutes == "")
                                {
                                    await SendMessage(client, messageMMI("Não entendi. Repita a hora por favor"));
                                }
                                else
                                {
                                    //// Send event hour
                                    IWebElement select_event_hour = driver.FindElement(By.Id("id_timestart_hour"));
                                    SelectElement select_hour = new SelectElement(select_event_hour);
                                    select_hour.SelectByValue(hour);

                                    //// Send event minutes 
                                    IWebElement select_event_minutes = driver.FindElement(By.Id("id_timestart_minute"));
                                    SelectElement select_minutes = new SelectElement(select_event_minutes);
                                    select_minutes.SelectByValue(minutes);

                                    //await SendMessage(client, messageMMI("Pretende confirmar ?"));

                                    IWebElement saveButton = driver.FindElement(By.CssSelector("button[data-action='save']"));
                                    saveButton.Click();
                                    await SendMessage(client, messageMMI("Evento criado"));
                                }

                            }
                        }

                        //if (intent == "affirm")
                        //{
                        //    //var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));
                        //    //IWebElement saveButton = wait.Until(x => x.FindElement(By.CssSelector("button[data-action='save']")));
                        //    //saveButton.Click();

                        //    IWebElement saveButton = driver.FindElement(By.CssSelector("button[data-action='save']"));
                        //    saveButton.Click();
                        //    await SendMessage(client, messageMMI("Evento criado"));

                        //    events_open= false;   
                        //}

                        if (intent == "logout")
                        {
                            if (myaccount)
                            {
                                IWebElement open_usermenu = driver.FindElement(By.Id("usermenu"));
                                open_usermenu.Click();

                                IWebElement click_logout = driver.FindElement(By.CssSelector("a[title='Sair']"));
                                click_logout.Click();

                                myaccount = false;
                            }
                            else
                            {
                                await SendMessage(client, messageMMI("Ainda não está autenticado !"));
                            }

                            if (!myaccount)
                            {
                                driver.Navigate().GoToUrl("https://elearning.ua.pt/");
                            }
                        }
                    }
                    else
                    {
                        await SendMessage(client, messageMMI("Não entendi. Repita por favor !"));
                    }
                }

                //IWebElement button_new_event = driver.FindElement(By.CssSelector("button[data-action='new-event-button']"));
                //if (!button_new_event.Enabled)
                //{
                //    popup_newevent = false;
                //}
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
