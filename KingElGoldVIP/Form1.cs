using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using System.Diagnostics;
using TLSharp.Core;
using WTelegram;
using Message = TL.Message;
using TL;
using System.Collections.Generic;
using System.IO;

namespace KingElGoldVIP
{
    public partial class Form1 : Form


    {
        private static HashSet<int> seenMessageIds = new HashSet<int>();
        private const string TargetGroup = "KING EL GOLD VIP \U0001f947"; // Your target group name
        //private const string TargetGroup = "Binance Crypto Box Codes"; // Your target group name
        private static Client client;
        private static Dictionary<long, TL.Channel> knownChats = new Dictionary<long, TL.Channel>();


        private TelegramBotClient botClient;

        public Form1()
        {
            InitializeComponent();
            InitializeBot();
            StartTelegramClient();

        }



        private async Task HandleUpdates(IObject update)
        {
            Updates updates = update as Updates;
            if (updates != null)
            {
                foreach (var upd in updates.UpdateList)
                {
                    await ProcessUpdate(upd);
                }
            }
            else
            {
                UpdateNewMessage singleUpdate = update as UpdateNewMessage;
                if (singleUpdate != null)
                {
                    await ProcessUpdate(singleUpdate);
                }
            }
        }

        private async Task ProcessUpdate(IObject upd)
        {
            UpdateNewMessage newMessage = upd as UpdateNewMessage;
            if (newMessage != null)
            {
                TL.Message msg = newMessage.message as TL.Message;
                if (msg != null && msg.Peer is PeerChannel peerChannel)
                {
                    if (!knownChats.ContainsKey(peerChannel.channel_id))
                    {
                        var dialogs = await client.Messages_GetAllDialogs();
                        foreach (var chat in dialogs.chats.Values)
                        {
                            TL.Channel ch = chat as TL.Channel;
                            if (ch != null && ch.title == TargetGroup)
                            {
                                knownChats[ch.id] = ch; // Store chat ID
                                break;
                            }
                        }
                    }

                    // Verify if the message belongs to the target group
                    TL.Channel channel;
                    if (knownChats.TryGetValue(peerChannel.channel_id, out channel))
                    {
                        if (!seenMessageIds.Contains(msg.ID))
                        {
                            seenMessageIds.Add(msg.ID);
                            Console.WriteLine($"[{channel.title}] {msg.message}");

                            // Append message to RichTextBox
                            if (checkBox1.Checked)
                            {
                                AnilizeSignal($"{msg.message}\n");
                            }

                            AppendTextToRichTextBox($"[{msg.message}\n");
                            // Process the message
                            //ProcessMessage(msg.message);
                        }
                    }
                }
            }
        }

        private static void ProcessMessage(string message)
        {
            // Your custom logic for handling the message
            Console.WriteLine($"Processing: {message}");
        }
        private async void InitializeBot()
        {
            string botToken = "7310694647:AAHTpgNwU25TpbcztzdS5ZR9aKfG4Kls59o"; // Replace with your bot's token
            botClient = new TelegramBotClient(botToken);

            // Start receiving updates
            var cts = new CancellationTokenSource();
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                cancellationToken: cts.Token
            );

            // Display bot information
            var botInfo = await botClient.GetMeAsync();
            checkedListBox1.Items.Add($"Connected as {botInfo.Username}");
        }
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text != null)
            {
                // Use Invoke to safely update the UI
                Invoke((Action)(() =>
                {
                    checkedListBox1.Items.Add($"[{update.Message.Chat.Username ?? "Unknown"}]: {update.Message.Text}");
                    //AnilizeSignal($"{update.Message.Text}");


                    // Append message to RichTextBox
                    if (checkBox1.Checked)
                    {
                        AnilizeSignal($"{update.Message.Text}\n");
                    }

                    AppendTextToRichTextBox($"[{update.Message.Chat.Username ?? "Unknown"}] {update.Message.Text}\n");

                }));


            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Log or display the error
            Invoke((Action)(() =>
            {
                //listBoxMessages.Items.Add($"Error: {exception.Message}");
            }));
            return Task.CompletedTask;
        }

        private void AnilizeSignal(string message)
        {
        

            // Regular expressions to extract data
            Regex tradeRegex = new Regex(@"(?<symbol>[A-Z]+)\s+(?<action>BUY|SELL)\s+NOW\s+(?<price>\d+)");
            Regex slRegex = new Regex(@"Sl\s*:\s*(?<sl>\d+)");
            Regex tpRegex = new Regex(@"Tp\s*:\s*(?<tp>\d+|open)", RegexOptions.Multiline);

            // Extract trade details
            Match tradeMatch = tradeRegex.Match(message);
            Match slMatch = slRegex.Match(message);
            MatchCollection tpMatches = tpRegex.Matches(message);

            if (tradeMatch.Success && tradeMatch.Groups["symbol"].Value == "XAUUSD")
            {
                string symbol = tradeMatch.Groups["symbol"].Value;
                string action = tradeMatch.Groups["action"].Value;
                string price = tradeMatch.Groups["price"].Value;
                int sl = slMatch.Success ? Convert.ToInt32(slMatch.Groups["sl"].Value) : 0;
                int tp = Convert.ToInt32(tpMatches[0].Groups["tp"].Value);

                switch (action)
                {
                    case "SELL":
                        sl += 1;
                        tp += 1;
                        creatOrder("XAUUSDm", 0.02, sl, tp, "SELL");
                        break;
                    case "BUY":
                        sl -= 1;
                        tp -= 1;
                        creatOrder("XAUUSDm", 0.02, sl, tp, "BUY");

                        break;
                    default:
                        break;
                }

                richTextBox2.Text += $"Symbol: {symbol}\n";
                richTextBox2.Text += $"Action: {action}\n";
                //richTextBox1.Text += $"Entry Price: {price}\n";
                richTextBox2.Text += $"Stop Loss: {sl}\n";

                richTextBox2.Text += "Take Profit:\n";

                richTextBox2.Text += $"{tp}\n";
                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
           
        }
        private void creatOrder(string symbol, double lotSize, double stopLoss, double takeProfit, string orderType)
        {
            // Example parameters to pass to the Python script
            //string symbol = "XAUUSDm";
            //double lotSize = 0.05;
            //double stopLoss = 2978;  // Example stop loss (price level)
            //double takeProfit = 2990; // Example take profit (price level)

            int magicNumber = 234001; // Dynamic magic number
            //string orderType = "BUY"; // You can use "SELL" for a sell order

            // Path to the Python executable
            string pythonPath = @"C:\Users\basha\AppData\Local\Programs\Python\Python313\python.exe";
            // Path to the Python script
            string scriptPath = @"C:\Users\basha\AppData\Local\Programs\Python\Python313\place_order_d.py";

            // Arguments to pass to the Python script
            string arguments = $"{symbol} {lotSize} {stopLoss} {takeProfit} {magicNumber} {orderType}";

            // Create a new process to call Python
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"{scriptPath} {arguments}",
                RedirectStandardOutput = true, // To capture output
                RedirectStandardError = true,  // Capture error output
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            Process process = new Process
            {
                StartInfo = start
            };

            process.Start();

            // Read the output from the Python script
            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd(); // Capture errors

            Console.WriteLine("Output: " + output);
            if (!string.IsNullOrEmpty(errors))
            {
                Console.WriteLine("Errors: " + errors);
            }

            process.WaitForExit();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

            // Example parameters to pass to the Python script
            string symbol = "BTCUSDm";
            double lotSize = 0.01;
            double stopLoss = 84800;  // Example stop loss (price level)
            double takeProfit = 82000; // Example take profit (price level)

            int magicNumber = 234001; // Dynamic magic number
            string orderType = "SELL"; // You can use "SELL" for a sell order

            // Path to the Python executable
            string pythonPath = @"C:\Users\basha\AppData\Local\Programs\Python\Python313\python.exe";
            // Path to the Python script
            string scriptPath = @"C:\Users\basha\AppData\Local\Programs\Python\Python313\place_order_d.py";

            // Arguments to pass to the Python script
            string arguments = $"{symbol} {lotSize} {stopLoss} {takeProfit} {magicNumber} {orderType}";

            // Create a new process to call Python
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"{scriptPath} {arguments}",
                RedirectStandardOutput = true, // To capture output
                RedirectStandardError = true,  // Capture error output
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            Process process = new Process
            {
                StartInfo = start
            };

            process.Start();

            // Read the output from the Python script
            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd(); // Capture errors

            Console.WriteLine("Output: " + output);
            if (!string.IsNullOrEmpty(errors))
            {
                Console.WriteLine("Errors: " + errors);
            }

            process.WaitForExit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }



        private async void StartTelegramClient()
        {
            client = new Client(Config);
            try
            {
                var myself = await client.LoginUserIfNeeded();
                AppendTextToRichTextBox($"Logged in as {myself.username ?? myself.first_name}.\n");
                //StartListeningForMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + ((TL.RpcException)ex).X, "Telegram Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            client.OnUpdates += HandleUpdates;

            // Keep the program running
            await Task.Delay(-1);


        }

        private static string Config(string what)
        {
            Console.WriteLine($"Telegram is asking for: {what}");

            if (what == "api_id")
            {
                return "23217044";  // Replace with your Telegram API ID

                //return "22288558";  // Replace with your Telegram API ID
            }
            else if (what == "api_hash")
            {
                return "d1e520d5e1c34c41d5869834beeff40a";
                //return "6cd390d27f6e5cadfe8c58351a1471ed";  // Replace with your Telegram API Hash
            }
            else if (what == "phone_number")
            {
                return "00962795717083";  // Replace with your Telegram phone number
               // return "00962799003729";  // Replace with your Telegram phone number
            }
            else if (what == "verification_code")
            {
                return PromptForCode();  // Ask user for the code sent by Telegram
            }
            else if (what == "password")
            {
                return PromptForPassword();  // If 2FA is enabled, ask for password
            }
            else
            {
                return null;
            }
        }


        private static string PromptForCode()
        {
            string code = "";
            InvokeIfNeeded(() =>
            {
                Form inputForm = new Form()
                {
                    Width = 300,
                    Height = 150,
                    Text = "Telegram Login",
                    StartPosition = FormStartPosition.CenterScreen
                };

                Label lbl = new Label() { Left = 20, Top = 20, Text = "Enter Telegram Code:" };
                TextBox txtBox = new TextBox() { Left = 20, Top = 50, Width = 200 };
                Button btnOk = new Button() { Text = "OK", Left = 20, Width = 80, Top = 80 };
                btnOk.DialogResult = DialogResult.OK;

                inputForm.Controls.Add(lbl);
                inputForm.Controls.Add(txtBox);
                inputForm.Controls.Add(btnOk);
                inputForm.AcceptButton = btnOk;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    code = txtBox.Text;
                }
            });
            return code;
        }

        private static string PromptForPassword()
        {
            string password = "";
            InvokeIfNeeded(() =>
            {
                Form inputForm = new Form()
                {
                    Width = 300,
                    Height = 150,
                    Text = "Telegram 2FA Password",
                    StartPosition = FormStartPosition.CenterScreen
                };

                Label lbl = new Label() { Left = 20, Top = 20, Text = "Enter Telegram Password:" };
                TextBox txtBox = new TextBox() { Left = 20, Top = 50, Width = 200, UseSystemPasswordChar = true };
                Button btnOk = new Button() { Text = "OK", Left = 20, Width = 80, Top = 80 };
                btnOk.DialogResult = DialogResult.OK;

                inputForm.Controls.Add(lbl);
                inputForm.Controls.Add(txtBox);
                inputForm.Controls.Add(btnOk);
                inputForm.AcceptButton = btnOk;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    password = txtBox.Text;
                }
            });
            return password;
        }

        private static void InvokeIfNeeded(Action action)
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(action);
                }
                else
                {
                    action();
                }
            }
            else
            {
                action();
            }
        }

private async void StartListeningForMessages()
{
    while (true)
    {
        // Get all chats (including channels, groups, and private chats)
        var chats = await client.Messages_GetAllChats();
        
        // Iterate over the chats using a simple foreach loop
        foreach (var pair in chats.chats)  // Accessing the KeyValuePair directly
        {
            var id = pair.Key;  // The chat ID
            var chat = pair.Value;  // The actual chat object




if (chat is TL.Channel channel && channel.title == "KING EL GOLD VIP \U0001f947")
{
    // Fetch message history
    var history = await client.Messages_GetHistory(channel, 0);

    foreach (var message in history.Messages)
    {
        if (message is TL.Message textMessage)
        {
            // Check if message ID is already in the HashSet
            if (!seenMessageIds.Contains(textMessage.ID))
            {
                // Add message ID to HashSet to mark it as processed
                seenMessageIds.Add(textMessage.ID);

                                    // Append message to RichTextBox
                                    if (checkBox1.Checked)
                                    {
                                        AnilizeSignal($"{textMessage.message}\n");
                                    }

                                    AppendTextToRichTextBox($"[{channel.Title}] {textMessage.message}\n");
    }
}
    }
}

        }
        await Task.Delay(120000); //5 min
    }
}

        private void AppendTextToRichTextBox(string text)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() => richTextBox1.AppendText(text)));
            }
            else
            {
                richTextBox1.AppendText(text);
            }
        }
    }
}
