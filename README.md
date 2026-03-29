# 💬 Telecommunication Chat System

A **real-time client–server chat application** developed using **C#, WPF, and TCP socket programming**.  
This system allows multiple users to connect to a server, authenticate, exchange private messages, and transfer files in real time.

This project demonstrates key **telecommunication and networking concepts**, including **client-server architecture, TCP communication, multithreading, and file transmission**.

---

# 📌 Project Overview

The system consists of two main components:

## 🖥 Chat Server
Responsible for:

- Accepting client connections
- Handling user authentication
- Routing messages between users
- Managing file transfers
- Maintaining the list of online users

## 💻 Chat Client
Provides the user interface for:

- Signing up and logging in
- Sending and receiving messages
- Viewing online users
- Sending and receiving files

All communication between clients is handled through the **TCP server**.

---

# ✨ Features

- 🔐 User **Sign Up and Login**
- 💬 **Private messaging**
- 🌍 **Public chat**
- 📁 **File transfer between users**
- 👥 **Online users list**
- ⚡ **Real-time communication**
- 🧵 **Multithreaded server**
- 📊 File receiving **progress display**
- 📦 File **size and sender information**

---

# 🛠 Technologies Used

| Technology | Purpose |
|------------|--------|
| **C#** | Application logic |
| **WPF** | User interface |
| **TCP Sockets** | Network communication |
| **Multithreading** | Handling multiple clients |
| **File I/O** | User data & file transfer |

---

# 🏗 System Architecture
Client A


→ TCP Server → Message Routing → Client B
/
/
Client C


1️⃣ Clients connect to the **TCP server**  
2️⃣ The server authenticates users  
3️⃣ Messages and files are routed through the server  
4️⃣ Clients receive messages in **real time**

---

## 📂 Project Structure

```
ChatApp
│
├── ChatClient
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   └── ChatClient.csproj
│
├── ChatServer
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── users.txt
│   └── ChatServer.csproj
│
├── ChatServer.slnx
├── .gitignore
└── README.md
```

## ▶️ How to Run the Application

### 1️⃣ Clone the repository

```bash
git clone https://github.com/yourusername/telecommunication-chat-system.git
```

### 2️⃣ Open the project

Open the solution file in **Visual Studio**

```
ChatServer.slnx
```

### 3️⃣ Start the server

Run the **ChatServer** project.

You should see:

```
Server started on port 5000
```

### 4️⃣ Start the clients

Run **ChatClient** multiple times to simulate multiple users.

### 5️⃣ Login or register

Example users:

```
Alice
Bob
```

### 6️⃣ Start chatting

You can now test:

- private messaging
- public messaging
- file transfer

---

## 📁 File Transfer Example

When a user sends a file:

**Sender view**

```
(File) You sent Codes.txt to Alice
(File) Size: 2.32 KB
```

**Receiver view**

```
Receiving file from Bob...
File Name: Codes.txt
File Size: 2.32 KB
Saved to: ReceivedFiles/Codes.txt
```

Files are stored inside:

```
ReceivedFiles/
```

---

## 🎓 Learning Objectives

This project demonstrates:

- TCP socket communication
- Client-server architecture
- Multithreading
- Message routing
- File transmission
- GUI development with WPF