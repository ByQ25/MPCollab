﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;

namespace MPCollab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private TwoCursorsHandler TCH;
        private bool disposed, hostOrClient; // true - host, false - client
        private static int timeWin = 17;

        public MainWindow()
        {
            InitializeComponent();

            //Dodanie skrótu i przypisanej metody
            RoutedCommand newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlSExecuted));

            newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.C, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlCExecuted));

            newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.V, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlVExecuted));

            disposed = false;

            try { this.localIPLabel.Content = GetLocalIPAddress(); }
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
            if (TCH == null) TCH = new TwoCursorsHandler(localIPLabel.Content.ToString(), timeWin, hostOrClient);
            TCH.StartServer();
            bottomLabel.Content = "Połączenie zosało nawiązane.";
            StartBlinking((Komputer)vb1.Child,(Komputer)vb2.Child);
            
        }

        // TODO: Some IP validation tool might proove to be useful here.
        private void ClientSideProcedure(byte computerTag)
        {
            hostOrClient = false;
            Mouse.OverrideCursor = Cursors.None;
            if (TCH == null)
            {
                switch (computerTag)
                {
                    case 0:
                        TCH = new TwoCursorsHandler(leftCompIPTB.Text, timeWin, hostOrClient);
                        StartBlinking((Komputer)vb1.Child, (Komputer)vb2.Child);
                        break;
                    case 1:
                        TCH = new TwoCursorsHandler(rightCompIPTB.Text, timeWin, hostOrClient);
                        StartBlinking((Komputer)vb2.Child, (Komputer)vb3.Child);
                        break;
                }
            }
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
            Komputer[] comps = { (Komputer)vb1.Child, (Komputer)vb2.Child, (Komputer)vb3.Child };
            StopBlinking(comps);
            bottomLabel.Content = "Połączenie zosało zakończone.";
        }

        // Events handling:
        private void MainWin_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.NumPad1: ServerSideProcedure(); break;
                case Key.NumPad2: ClientSideProcedure(0); break;
                case Key.NumPad3: ClientSideProcedure(1); break;
                case Key.Escape: RestoreAppToInitialState(); break;
            }
        }

        private void buttonHost_Click(object sender, RoutedEventArgs e)
        {
            ServerSideProcedure();
        }

        private void buttonClientLeft_Click(object sender, RoutedEventArgs e)
        {
            ClientSideProcedure(0);
        }

        private void buttonClientRight_Click(object sender, RoutedEventArgs e)
        {
            ClientSideProcedure(1);
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

        private void ControlCExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (TCH != null && !hostOrClient) TCH.HandleCopy();
        }

        private void ControlVExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (TCH != null && !hostOrClient) TCH.HandlePaste();
        }

        private void ControlSExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("skrót");
        }

        private void StartBlinking(Komputer com1, Komputer com2)
        {
            com1.Start();
            com2.Start();
        }

        private void StopBlinking(Komputer[] comps)
        {
            foreach (Komputer comp in comps)
                comp.Stop();
        }

        // IDisposable implementation:
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing) TCH.Dispose();
            this.disposed = true;
        }
    }
}
