using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        StreamReader reader;
        StreamWriter writer;
        bool isLoggedIn = false;
        string currentUsername = "";

        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
        }

        void ConnectToServer()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 5000);
                NetworkStream stream = client.GetStream();

                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                Thread thread = new Thread(ReceiveMessages);
                thread.IsBackground = true;
                thread.Start();
            }
            catch
            {
                System.Windows.MessageBox.Show("Cannot connect to server");
            }
        }

        void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    string? message = reader.ReadLine();

                    if (message == null)
                        break;

                    Dispatcher.Invoke(() =>
                    {
                        if (message == "SIGNUP_SUCCESS")
                        {
                            ChatBox.AppendText("Sign up successful.\n");
                            ChatBox.ScrollToEnd();
                        }
                        else if (message == "SIGNUP_FAIL")
                        {
                            ChatBox.AppendText("Sign up failed. Username may already exist.\n");
                            ChatBox.ScrollToEnd();
                        }
                        else if (message == "LOGIN_SUCCESS")
                        {
                            isLoggedIn = true;
                            SendButton.IsEnabled = true;
                            SendFileButton.IsEnabled = true;

                            UsernameBox.IsEnabled = false;
                            PasswordBox.IsEnabled = false;

                            LoginButton.IsEnabled = false;
                            SignUpButton.IsEnabled = false;

                            ChatBox.AppendText("Login successful as " + currentUsername + ".\n");
                            ChatBox.ScrollToEnd();
                        }
                        else if (message == "LOGIN_FAIL")
                        {
                            isLoggedIn = false;
                            SendButton.IsEnabled = false;
                            SendFileButton.IsEnabled = false;
                            ChatBox.AppendText("Login failed.\n");
                            ChatBox.ScrollToEnd();
                        }
                        else if (message.StartsWith("USERLIST|"))
                        {
                            string usersPart = message.Substring(9);
                            UsersListBox.Items.Clear();

                            if (usersPart.Length > 0)
                            {
                                string[] users = usersPart.Split(',');

                                foreach (string user in users)
                                {
                                    if (user != currentUsername)
                                        UsersListBox.Items.Add(user);
                                }
                            }
                        }
                        else if (message.StartsWith("FILE|"))
                        {
                            string[] parts = message.Split('|');

                            if (parts.Length == 4)
                            {
                                string sender = parts[1];
                                string fileName = parts[2];
                                string base64Data = parts[3];

                                byte[] fileBytes = Convert.FromBase64String(base64Data);
                                double fileSizeKB = fileBytes.Length / 1024.0;

                                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReceivedFiles");

                                if (!Directory.Exists(folderPath))
                                {
                                    Directory.CreateDirectory(folderPath);
                                }

                                string savePath = Path.Combine(folderPath, fileName);
                                File.WriteAllBytes(savePath, fileBytes);

                                ChatBox.AppendText("(File) Received from " + sender + ": " + fileName + "\n");
                                ChatBox.AppendText("(File) Size: " + fileSizeKB.ToString("F2") + " KB\n");
                                ChatBox.AppendText("(File) Saved to: " + savePath + "\n");
                                ChatBox.ScrollToEnd();
                            }
                        }
                        else
                        {
                            ChatBox.AppendText(message + "\n");
                            ChatBox.ScrollToEnd();
                        }
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    ChatBox.AppendText("Disconnected from server\n");
                    ChatBox.ScrollToEnd();
                });
            }
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text.Trim();
                string password = PasswordBox.Password.Trim();

                if (username == "" || password == "")
                {
                    System.Windows.MessageBox.Show("Please enter username and password");
                    return;
                }

                writer.WriteLine("SIGNUP|" + username + "|" + password);
                writer.Flush();
            }
            catch
            {
                System.Windows.MessageBox.Show("Sign up error");
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text.Trim();
                string password = PasswordBox.Password.Trim();

                if (username == "" || password == "")
                {
                    System.Windows.MessageBox.Show("Please enter username and password");
                    return;
                }

                currentUsername = username;
                writer.WriteLine("LOGIN|" + username + "|" + password);
                writer.Flush();
            }
            catch
            {
                System.Windows.MessageBox.Show("Login error");
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!isLoggedIn)
            {
                System.Windows.MessageBox.Show("Please login first");
                return;
            }

            try
            {
                string text = MessageInput.Text.Trim();

                if (text == "")
                    return;

                if (UsersListBox.SelectedItem != null)
                {
                    string receiver = UsersListBox.SelectedItem.ToString()!;
                    writer.WriteLine("PRIVATE|" + currentUsername + "|" + receiver + "|" + text);

                    ChatBox.AppendText("(Private to " + receiver + ") " + currentUsername + ": " + text + "\n");
                    ChatBox.ScrollToEnd();
                }
                else
                {
                    writer.WriteLine("MSG|" + currentUsername + "|" + text);
                }

                writer.Flush();
                MessageInput.Clear();
            }
            catch
            {
                System.Windows.MessageBox.Show("Error sending message");
            }
        }

        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            if (!isLoggedIn)
            {
                System.Windows.MessageBox.Show("Please login first");
                return;
            }

            if (UsersListBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Please select a user to send the file to");
                return;
            }

            try
            {
                OpenFileDialog dialog = new OpenFileDialog();

                if (dialog.ShowDialog() == true)
                {
                    string receiver = UsersListBox.SelectedItem.ToString()!;
                    string filePath = dialog.FileName;
                    string fileName = Path.GetFileName(filePath);
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string base64Data = Convert.ToBase64String(fileBytes);

                    writer.WriteLine("FILE|" + currentUsername + "|" + receiver + "|" + fileName + "|" + base64Data);
                    writer.Flush();

                    long fileSizeBytes = fileBytes.Length;
                    double fileSizeKB = fileSizeBytes / 1024.0;

                    ChatBox.AppendText("(File) You sent " + fileName + " to " + receiver + "\n");
                    ChatBox.AppendText("(File) Size: " + fileSizeKB.ToString("F2") + " KB\n");
                    ChatBox.ScrollToEnd();
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Error sending file");
            }
        }

        private void PublicChat_Click(object sender, RoutedEventArgs e)
        {
            UsersListBox.SelectedItem = null;
        }
    }
}