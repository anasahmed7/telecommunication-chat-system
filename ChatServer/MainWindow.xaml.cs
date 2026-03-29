using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace ChatServer
{
    public partial class MainWindow : Window
    {
        TcpListener? server;
        List<TcpClient> clients = new List<TcpClient>();
        Dictionary<string, string> users;
        Dictionary<TcpClient, string> loggedInUsers = new Dictionary<TcpClient, string>();

        public MainWindow()
        {
            InitializeComponent();
            users = LoadUsers();
            StartServer();
        }

        Dictionary<string, string> LoadUsers()
        {
            Dictionary<string, string> loadedUsers = new Dictionary<string, string>();

            if (File.Exists("users.txt"))
            {
                string[] lines = File.ReadAllLines("users.txt");

                foreach (string line in lines)
                {
                    string[] parts = line.Split(':');

                    if (parts.Length == 2)
                    {
                        string username = parts[0];
                        string password = parts[1];
                        loadedUsers[username] = password;
                    }
                }
            }

            return loadedUsers;
        }

        void SaveUsers()
        {
            List<string> lines = new List<string>();

            foreach (var user in users)
            {
                lines.Add(user.Key + ":" + user.Value);
            }

            File.WriteAllLines("users.txt", lines);
        }

        void Log(string text)
        {
            Dispatcher.Invoke(() =>
            {
                LogBox.AppendText(text + "\n");
                LogBox.ScrollToEnd();
            });
        }

        void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();

            Log("Server started on port 5000");

            Thread thread = new Thread(ListenForClients);
            thread.IsBackground = true;
            thread.Start();
        }

        void ListenForClients()
        {
            while (true)
            {
                TcpClient client = server!.AcceptTcpClient();
                Log("Client connected");

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
        }

        void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            bool loggedIn = false;

            try
            {
                while (true)
                {
                    string? message = reader.ReadLine();

                    if (message == null)
                        break;

                    string[] parts = message.Split('|');

                    if (parts.Length >= 1)
                    {
                        if (parts[0] == "SIGNUP" && parts.Length == 3)
                        {
                            string username = parts[1];
                            string password = parts[2];

                            if (!users.ContainsKey(username))
                            {
                                users[username] = password;
                                SaveUsers();

                                writer.WriteLine("SIGNUP_SUCCESS");
                                writer.Flush();

                                Log("New user signed up: " + username);
                            }
                            else
                            {
                                writer.WriteLine("SIGNUP_FAIL");
                                writer.Flush();

                                Log("Signup failed for username: " + username);
                            }
                        }
                        else if (parts[0] == "LOGIN" && parts.Length == 3)
                        {
                            string username = parts[1];
                            string password = parts[2];

                            if (users.ContainsKey(username) && users[username] == password)
                            {
                                loggedIn = true;

                                if (!clients.Contains(client))
                                    clients.Add(client);

                                if (!loggedInUsers.ContainsKey(client))
                                    loggedInUsers.Add(client, username);

                                writer.WriteLine("LOGIN_SUCCESS");
                                writer.Flush();

                                Log(username + " logged in");
                                SendUserListToAll();
                            }
                            else
                            {
                                writer.WriteLine("LOGIN_FAIL");
                                writer.Flush();

                                Log("Login failed for username: " + username);
                            }
                        }
                        else if (parts[0] == "MSG" && parts.Length == 3 && loggedIn)
                        {
                            string username = parts[1];
                            string text = parts[2];

                            string fullMessage = username + ": " + text;
                            Log(fullMessage);
                            Broadcast(fullMessage);
                        }
                        else if (parts[0] == "PRIVATE" && parts.Length == 4 && loggedIn)
                        {
                            string sender = parts[1];
                            string receiver = parts[2];
                            string text = parts[3];

                            SendPrivateMessage(sender, receiver, text);
                        }
                        else if (parts[0] == "FILE" && parts.Length == 5 && loggedIn)
                        {
                            string sender = parts[1];
                            string receiver = parts[2];
                            string fileName = parts[3];
                            string base64Data = parts[4];

                            SendFile(sender, receiver, fileName, base64Data);
                        }
                    }
                }
            }
            catch
            {
                Log("Connection error");
            }
            finally
            {
                clients.Remove(client);

                if (loggedInUsers.ContainsKey(client))
                {
                    string username = loggedInUsers[client];
                    loggedInUsers.Remove(client);
                    Log(username + " disconnected");
                    SendUserListToAll();
                }
                else
                {
                    Log("Client disconnected");
                }

                client.Close();
            }
        }

        void Broadcast(string message)
        {
            foreach (var client in clients)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(client.GetStream());
                    writer.WriteLine(message);
                    writer.Flush();
                }
                catch
                {
                }
            }
        }

        void SendPrivateMessage(string sender, string receiver, string text)
        {
            foreach (var pair in loggedInUsers)
            {
                TcpClient client = pair.Key;
                string username = pair.Value;

                if (username == receiver)
                {
                    try
                    {
                        StreamWriter writer = new StreamWriter(client.GetStream());
                        writer.WriteLine("(Private) " + sender + ": " + text);
                        writer.Flush();

                        Log("(Private) " + sender + " -> " + receiver + ": " + text);
                    }
                    catch
                    {
                    }

                    break;
                }
            }
        }

        void SendFile(string sender, string receiver, string fileName, string base64Data)
        {
            foreach (var pair in loggedInUsers)
            {
                TcpClient client = pair.Key;
                string username = pair.Value;

                if (username == receiver)
                {
                    try
                    {
                        StreamWriter writer = new StreamWriter(client.GetStream());
                        writer.WriteLine("FILE|" + sender + "|" + fileName + "|" + base64Data);
                        writer.Flush();

                        Log("(File) " + sender + " -> " + receiver + ": " + fileName);
                    }
                    catch
                    {
                    }

                    break;
                }
            }
        }

        void SendUserListToAll()
        {
            string userList = "";

            foreach (var username in loggedInUsers.Values)
            {
                if (userList == "")
                    userList = username;
                else
                    userList += "," + username;
            }

            foreach (var client in clients)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(client.GetStream());
                    writer.WriteLine("USERLIST|" + userList);
                    writer.Flush();
                }
                catch
                {
                }
            }
        }
    }
}