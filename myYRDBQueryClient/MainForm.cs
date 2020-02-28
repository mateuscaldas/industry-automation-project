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
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace Quickstarts.myYRDBQueryClient
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
            ConnectServerCTRL.ServerUrl = "opc.tcp://localhost:62541/Quickstarts/ReferenceServer";
            this.Text = m_configuration.ApplicationName;
        }
        #endregion

        #region Private Fields
        private ApplicationConfiguration m_configuration;
        private Session m_session;
        private bool m_connectedOnce;

        private Subscription m_subscription;      
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

                // browse the instances in the server.
                //BrowseCTRL.Initialize(m_session, ObjectIds.ObjectsFolder, ReferenceTypeIds.Organizes, ReferenceTypeIds.Aggregates);

                //verify if Session is established
                if (m_session != null)
                {
                    //verify if the Subscription was not already created
                    if (m_subscription == null)
                    {
                        //create the subscritpion
                        m_subscription = new Subscription(m_session.DefaultSubscription);
                        m_subscription.PublishingEnabled = true;
                        m_subscription.PublishingInterval = 1000; //1000msec interval
                        m_session.AddSubscription(m_subscription);
                        m_subscription.Create();
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
        }

        /// <summary>
        /// Updates the application after a communicate error was detected.
        /// </summary>
        private void Server_ReconnectStarting(object sender, EventArgs e)
        {
            try
            {
                //BrowseCTRL.ChangeSession(null);
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
                //BrowseCTRL.ChangeSession(m_session);
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
            if (MessageBox.Show("Exit this application?", "my YRDBQueryClient", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
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

        private async void button1_Click(object sender, EventArgs e)
        {
            //Start the production of the piece
            DataValueCollection results = Read_Value("ns=3;s=Piece_Info");
            bool Start_PieceValue = (bool)results[0].Value;            
            try
            {
                bool arr = true;
                WriteValue valueToWrite = new WriteValue();
                valueToWrite.NodeId = "ns=3;s=Piece_Info";
                valueToWrite.AttributeId = Attributes.Value;
                valueToWrite.Value.Value = arr;
                valueToWrite.Value.StatusCode = StatusCodes.Good;
                valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                WriteValueCollection valuesToWrite = new WriteValueCollection();
                valuesToWrite.Add(valueToWrite);

                // write current value.
                StatusCodeCollection results2 = null;
                DiagnosticInfoCollection diagnosticInfos2 = null;

                m_session.Write(
                null,
                valuesToWrite,
                out results2,
                out diagnosticInfos2);
                ClientBase.ValidateResponse(results2, valuesToWrite);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                if (StatusCode.IsBad(results2[0]))
                {
                    throw new ServiceResultException(results2[0]);
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException("Error Writing Value", exception);
            }
            await Task.Delay(1000);
            try
            {
                bool arr = false;
                WriteValue valueToWrite = new WriteValue();
                valueToWrite.NodeId = "ns=3;s=Piece_Info";
                valueToWrite.AttributeId = Attributes.Value;
                valueToWrite.Value.Value = arr;
                valueToWrite.Value.StatusCode = StatusCodes.Good;
                valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                WriteValueCollection valuesToWrite = new WriteValueCollection();
                valuesToWrite.Add(valueToWrite);

                // write current value.
                StatusCodeCollection results2 = null;
                DiagnosticInfoCollection diagnosticInfos2 = null;

                m_session.Write(
                null,
                valuesToWrite,
                out results2,
                out diagnosticInfos2);
                ClientBase.ValidateResponse(results2, valuesToWrite);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                if (StatusCode.IsBad(results2[0]))
                {
                    throw new ServiceResultException(results2[0]);
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException("Error Writing Value", exception);
            }

            DataValueCollection read = Read_Value("ns=3;s=Mould_SN");
            label8.Text = (string)read[0].Value;

            DataValueCollection read1 = Read_Value("ns=3;s=Time");
            label4.Text = (string)read1[0].Value;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (Int32.TryParse(textBox1.Text, out int j))
            {
                try
                {
                    int arr = j;
                    WriteValue valueToWrite = new WriteValue();
                    valueToWrite.NodeId = "ns=3;s=Piece_SN";
                    valueToWrite.AttributeId = Attributes.Value;
                    valueToWrite.Value.Value = arr;
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results2 = null;
                    DiagnosticInfoCollection diagnosticInfos2 = null;

                    m_session.Write(
                    null,
                    valuesToWrite,
                    out results2,
                    out diagnosticInfos2);
                    ClientBase.ValidateResponse(results2, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                    if (StatusCode.IsBad(results2[0]))
                    {
                        throw new ServiceResultException(results2[0]);
                    }
                }
                catch (Exception exception)
                {
                    ClientUtils.HandleException("Error Writing Value", exception);
                }
            }

        }
    }

}

