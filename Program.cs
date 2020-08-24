using System;

namespace Logistic_Bot
{
    class Program
    {
        static void Main(string[] args)
        {
            var bot = new Bot();
            bot.RunAsync().GetAwaiter().GetResult(); //Doesn't wait for this to finish, it continues
        }
    }
}
