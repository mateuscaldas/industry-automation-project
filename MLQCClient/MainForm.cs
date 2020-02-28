/* ========================================================================
 * Copyright (c) 2005-2019 The my Company, Inc. All rights reserved.
 *
 * my Company MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using System.IO;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using System.Globalization;
//Add MySql Library
using InfluxDB.LineProtocol.Payload;
using InfluxDB.LineProtocol.Client;

namespace Quickstarts.MLQCClient
{
    /// <summary>
    /// The main form for a simple Quickstart Client application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Constructors
        /// <summary>
        /// Creates an empty form.
        /// </summary>
        private MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a form which uses the specified client configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        public MainForm(ApplicationConfiguration configuration)
        {
            InitializeComponent();
            ConnectServerCTRL.Configuration = m_configuration = configuration;
            ConnectServerCTRL.ServerUrl = "opc.tcp://desktop-6oo2d8b:62545/Quickstarts/myIMMControlServer";

            this.Text = m_configuration.ApplicationName;
        }
        #endregion

        #region Private Fields
        private ApplicationConfiguration m_configuration;
        private Session m_session;
        private bool m_connectedOnce;

        private Subscription m_subscription;
        
        private double[] Pressure_1 = new double[28];
        private double[] Pressure_2 = new double[28];
        private double[] Pressure_Array;
        private int Mould_Type;

        private uint Pressure_Array_handle;
        private uint Mould_Type_handle;
        //DB       
        private string DB_server = "http://localhost:8086";
        private string DB_database = "mytsdb_MLQC";
        private uint Actual_Mould_handle;
        #endregion

        #region Private Methods
        #endregion

        #region Event Handlers
        /// <summary>
        /// Connects to a server.
        /// </summary>
        private async void Server_ConnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                await ConnectServerCTRL.Connect();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Disconnects from the current session.
        /// </summary>
        private void Server_DisconnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Disconnect();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Prompts the user to choose a server on another host.
        /// </summary>
        private void Server_DiscoverMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Discover(null);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after connecting to or disconnecting from the server.
        /// </summary>
        private void Server_ConnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = ConnectServerCTRL.Session;

                // set a suitable initial state.
                if (m_session != null && !m_connectedOnce)
                {
                    m_connectedOnce = true;
                }

                //verify if Session is established
                if (m_session != null)
                {
                    //verify if the Subscription was not already created
                    if (m_subscription == null)
                    {
                        //create the subscritpion
                        m_subscription = new Subscription(m_session.DefaultSubscription);
                        m_subscription.PublishingEnabled = true;
                        m_subscription.PublishingInterval = 4000; //msec interval
                        m_session.AddSubscription(m_subscription);
                        m_subscription.Create();
                        MonitoredItem monitoredItem;

                        //Add Pressure_array to monitored items
                        monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                        monitoredItem.StartNodeId = "ns=3;s=Sensors_Data";
                        monitoredItem.AttributeId = Attributes.Value;
                        monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                        monitoredItem.SamplingInterval = 1000;
                        monitoredItem.QueueSize = 0;
                        monitoredItem.DiscardOldest = true;
                        //get handle number
                        Pressure_Array_handle = monitoredItem.ClientHandle;
                        //define event handler for this item, and then add to subscription
                        monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                        m_subscription.AddItem(monitoredItem);
                        m_subscription.ApplyChanges();


                        //Add Actual_Mould to monitored items
                        monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                        monitoredItem.StartNodeId = "ns=3;s=Actual_Mould";
                        monitoredItem.AttributeId = Attributes.Value;
                        monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                        monitoredItem.SamplingInterval = 1000;
                        monitoredItem.QueueSize = 0;
                        monitoredItem.DiscardOldest = true;
                        //get handle number
                        Actual_Mould_handle = monitoredItem.ClientHandle;
                        //define event handler for this item, and then add to subscription
                        monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                        m_subscription.AddItem(monitoredItem);
                        m_subscription.ApplyChanges();
                    }
                    
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
            
        }

        void monitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            //This part of the code is needed to manage issues related to windows forms 
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MonitoredItemNotificationEventHandler(monitoredItem_Notification), monitoredItem, e);
                return;
            }
            MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;
            if (notification == null)
            {
                return;
            }
            //PROJECT 

            //updating the Mould used (to structure the Database)
            //is call because of Mould_Type?
            if (monitoredItem.ClientHandle == Mould_Type_handle)
            {
                int value = (int)notification.Value.WrappedValue.Value;
                Mould_Type = value;
                //update screen
                label5.Text = "value: " + Utils.Format("{0}", Mould_Type.ToString()) +
                ";\nStatusCode: " + Utils.Format("{0}", notification.Value.StatusCode.ToString()) +
                ";\nSource timestamp: " + notification.Value.SourceTimestamp.ToString() +
                ";\nServer timestamp: " + notification.Value.ServerTimestamp.ToString();
            }

            //is call because of Pressure_Array?
            DataValueCollection read2 = Read_Value("ns=3;s=Start_Piece");
            if (monitoredItem.ClientHandle == Pressure_Array_handle && (bool)read2[0].Value)
            {
                double[] value = (double[])notification.Value.WrappedValue.Value;
                Pressure_Array = value;
                for (int i = 0; i< 28; i++)
                {
                    Pressure_1[i] = Pressure_Array[i];
                    Pressure_2[i] = Pressure_Array[i+28];
                }
                label1.Text = Pressure_1[1].ToString();

                DataValueCollection read = Read_Value("ns=3;s=Actual_Mould");
                DataValueCollection read1 = Read_Value("ns=3;s=Piece_SN");

                if ((int)read[0].Value == 1)
                {
                    var payload = new LineProtocolPayload();
                    //when new value of Pressure_1 then update DB
                    for (int j = 0; j < 28; j++)
                    {
                        DateTime t = DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds((27 - j) * 285));
                        var DBMould1 = new LineProtocolPoint(
                         "Mould_1__MLQC_DB",
                         new Dictionary<string, object>
                         {
                            { "Pressure 1", Pressure_1[j]},
                            { "Pressure 2", Pressure_2[j]},
                            {"Piece Serial Number", (int)read1[0].Value}
                         },
                         null,
                         t
                         );                        
                        payload.Add(DBMould1);
                    }

                    var client = new LineProtocolClient(new Uri(DB_server), DB_database);
                    //send data to DB
                    var influxResult = client.WriteAsync(payload);
                }
                
                if ((int)read[0].Value == 2)
                {
                    var payload = new LineProtocolPayload();
                    //when new value of Pressure_1 then update DB
                    for (int j = 0; j < 28; j++)
                    {
                        DateTime t = DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds((27 - j) * 50));
                        var DBMould1 = new LineProtocolPoint(
                         "Mould_2__MLQC_DB",
                         new Dictionary<string, object>
                         {
                            { "Pressure 1 =", Pressure_1[j]},
                            { "Pressure 2 =", Pressure_2[j]}
                         },
                         null,
                         t
                         );
                        payload.Add(DBMould1);
                    }

                    var client = new LineProtocolClient(new Uri(DB_server), DB_database);
                    //send data to DB
                    var influxResult = client.WriteAsync(payload);
                }
            }
        }

        /// <summary>
        /// Updates the application after a communicate error was detected.
        /// </summary>
        private void Server_ReconnectStarting(object sender, EventArgs e)
        {
            try
            {
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after reconnecting to the server.
        /// </summary>
        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = ConnectServerCTRL.Session;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Cleans up when the main form closes.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ConnectServerCTRL.Disconnect();
        }
        #endregion

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit this application?", "MLQCClient", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void Help_ContentsMI_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start( Path.GetDirectoryName(Application.ExecutablePath) + "\\WebHelp\\overview_-_reference_client.htm");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to launch help documentation. Error: " + ex.Message);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {
                
        }

        private void clientHeaderBranding1_Load(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private DataValueCollection Read_Value(string node)
        {
            //Read variables in the server’s address space
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
            ReadValueId nodeToRead = new ReadValueId();
            nodeToRead.NodeId = node;
            nodeToRead.AttributeId = Attributes.Value;
            nodesToRead.Add(nodeToRead);

            DataValueCollection results = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            m_session.Read(
            null,
            0,
            TimestampsToReturn.Neither,
            nodesToRead,
            out results,
            out diagnosticInfos);

            ClientBase.ValidateResponse(results, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            return results;
        }

    }
}
