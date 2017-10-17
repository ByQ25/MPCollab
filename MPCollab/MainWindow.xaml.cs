﻿using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MPCollab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TwoCursorsHandler TCH;
        private bool hostOrClient; // true - host, false - client
        private static int timeWin = 17;

        public MainWindow()
        {
            InitializeComponent();

            //Dodanie skrótu i przypisanej metody
            RoutedCommand newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlSExecuted));
            
            try { this.textBox.Text = GetLocalIPAddress(); }
            catch (ApplicationException) { }
        }

        // Additional methods:
        private static string GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            throw new ApplicationException("Local IP Address Not Found!");
        }

        private void ServerSideProcedure()
        {
            hostOrClient = true;
            if (TCH == null) TCH = new TwoCursorsHandler(textBox.Text, timeWin, hostOrClient);
            TCH.StartServer();
            bottomLabel.Content = "Połączenie zosało nawiązane.";
            StartBlinking((Komputer)vb1.Child,(Komputer)vb2.Child);
            
        }

        private void ClientSideProcedure()
        {
            hostOrClient = false;
            Mouse.OverrideCursor = Cursors.None;
            if (TCH == null) TCH = new TwoCursorsHandler(textBox.Text, timeWin, hostOrClient);
            StartBlinking((Komputer)vb1.Child, (Komputer)vb2.Child);
        }

        private void RestoreAppToInitialState()
        {
            if (TCH != null)
            {
                TCH.StopServer();
                TCH.Dispose();
                TCH = null;
            }
            Mouse.OverrideCursor = Cursors.Arrow;
            StopBlinking((Komputer)vb1.Child, (Komputer)vb2.Child);
            bottomLabel.Content = "Połączenie zosało zakończone.";
        }

        // Events handling:
        private void MainWin_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.NumPad1: ServerSideProcedure(); break;
                case Key.NumPad2: ClientSideProcedure(); break;
                case Key.Escape: RestoreAppToInitialState(); break;
            }
        }

        private void buttonHost_Click(object sender, RoutedEventArgs e)
        {
            ServerSideProcedure();
        }

        private void buttonClient_Click(object sender, RoutedEventArgs e)
        {
            ClientSideProcedure();
        }

        private void MainWin_MouseMove(object sender, MouseEventArgs e)
        {
            if (TCH != null) TCH.HandleMouseMove();
        }

        private void MainWin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TCH != null) TCH.HandleMouseClick(TwoCursorsHandler.MButtons.LMB);
        }

        private void MainWin_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TCH != null) TCH.HandleMouseClick(TwoCursorsHandler.MButtons.RMB);
        }

        private void ControlSExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Wow, działa");
        }
        private void StartBlinking(Komputer com1, Komputer com2)
        {
            com1.Start();
            com2.Start();
        }
        private void StopBlinking(Komputer com1, Komputer com2)
        {
            com1.Stop();
            com2.Stop();
        }
    }
}
