using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DemoChatApp
{
    internal class Program
    {
        static readonly string conString = "Endpoint=sb://servicebus-ab.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XI8QERj0TzscRBm+l4FrRmWRdkRGmC6ouKncXeLPkEg=";
        static readonly string topicPath = "chattopic";
        static void Main(string[] args)
        {
            Console.WriteLine("********************* DEMO CHAT ********************");
            Console.WriteLine("");
            Console.WriteLine("");
            

            //Create a management client to manage artifacts
            var manager = new ManagementClient(conString);

            // Create a topic if not exists
            if(!manager.TopicExistsAsync(topicPath).Result)
                manager.CreateTopicAsync(topicPath).Wait();

            // Ask user to enter valid subscription/username
            SubscriptionName:

            Console.WriteLine("Enter Name:");
            var userName = Console.ReadLine();
            if(manager.SubscriptionExistsAsync(topicPath, userName).Result)
            {
                Console.WriteLine("User Name already exists. Please enter valid User Name");
                goto SubscriptionName;
            }
                

            // Create a subscription for the user
            var description = new SubscriptionDescription(topicPath, userName) 
            { 
                AutoDeleteOnIdle = TimeSpan.FromMinutes(5)
            };
            manager.CreateSubscriptionAsync(description).Wait();

            // Create Clients
            var topicClient = new TopicClient(conString, topicPath);
            var subscriptionClient = new SubscriptionClient(conString, topicPath, userName);

            // Create Subscription puump for recieving messages
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionRecievedHandelerAsync)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
            subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);

            //Send a Welcome Message on Entering in chat
            var welcomeMessage = new Message(Encoding.UTF8.GetBytes("Is Avaliable For Chat..."));
            welcomeMessage.Label = userName;
            topicClient.SendAsync(welcomeMessage).Wait();

            while (true)
            {
                string text = Console.ReadLine();
                if (text.Equals("exit"))
                    break;

                //Send Message
                var chatMessage = new Message(Encoding.UTF8.GetBytes(text));
                chatMessage.Label = userName;
                topicClient.SendAsync(chatMessage).Wait();
            }

            // Send Message When User leaves chat window
            var goodbyeMessage = new Message(Encoding.UTF8.GetBytes("Is Leave the chat..."));
            goodbyeMessage.Label = userName;
            topicClient.SendAsync(goodbyeMessage).Wait();


            // Close the clients
            topicClient.CloseAsync().Wait();
            subscriptionClient.CloseAsync().Wait();


        }

        private static async Task ExceptionRecievedHandelerAsync(ExceptionReceivedEventArgs ex)
        {
            Console.WriteLine("oops... someting goes wrong");
            Console.WriteLine(ex.Exception.Message);
        }

        private static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Deserialize messaage body from bytes
            var text = Encoding.UTF8.GetString(message.Body);
            Console.WriteLine($"{ message.Label } > {text}");
        }
    }
}
