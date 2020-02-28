/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
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

 //Luis Perdigoto - DEE-ESTG-IPLeiria



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
//Add MySql Library
using MySql.Data.MySqlClient;
using System.Globalization;

namespace Opc.Ua.Di
{
    public class myYRDBQueryNodeManager : Quickstarts.myYRDBQuery.EmptyNodeManager
    {
        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public myYRDBQueryNodeManager(
            Opc.Ua.Server.IServerInternal server,
            ApplicationConfiguration configuration)
        :
            base(server, configuration)
        {
            List<string> namespaceUris = new List<string>();

            //add namespace for DI nodeset and add namespace DI/Instance for my variables
            namespaceUris.Add(Namespaces.OpcUaDi);
            namespaceUris.Add(Namespaces.OpcUaDi + "/Instance");

            NamespaceUris = namespaceUris;

            

            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[1]);
            m_lastUsedId = 0;

        }
        #endregion

        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            uint id = Utils.IncrementIdentifier(ref m_lastUsedId);
            return new NodeId(id, m_namespaceIndex);
        }
        #endregion

        #region INodeManager Members
        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();            
            predefinedNodes.LoadFromBinaryResource(context, 
                "Opc.Ua.Di.PredefinedNodes.uanodes", this.GetType().GetTypeInfo().Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                //Load DI nodeset
                LoadPredefinedNodes(SystemContext, externalReferences);

                //Add my variables to Address Space, under the DeviceSet object -----------------------
                //-------------------------------------------------------------------------------------

                //get the DeviceSet object of the Address Space
                NodeState DeviceSetobj = (NodeState)FindPredefinedNode(
                    ExpandedNodeId.ToNodeId(ObjectIds.DeviceSet, Server.NamespaceUris), typeof(NodeState));

                //-------------------------------------------------------------------------------------
                //create myDevice1 as a simple folder with a variable                
                FolderState mydev1 = CreateFolder(DeviceSetobj, "myDevice1", "myDevice1");
                                
                //create myDevice1/Piece_Info
                Piece_Info = CreateVariable(mydev1, "Piece_Info", "Piece_Info", Opc.Ua.DataTypeIds.Boolean, ValueRanks.Scalar);

                // create myDevice1/Piece_SN
                Piece_SN = CreateVariable(mydev1, "Piece_SN", "Piece_SN", Opc.Ua.DataTypeIds.Int32, ValueRanks.Scalar);

                // create myDevice1/Mould_SN
                Mould_SN = CreateVariable(mydev1, "Mould_SN", "Mould_SN", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                // create myDevice1/Time
                Time = CreateVariable(mydev1, "Time", "Time", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                //initialize var
                Piece_Info.Value = (bool)false;
                Piece_SN.Value = (int)4;
                Mould_SN.Value = (string)"-";
                Time.Value = (string)"-";

                //add to Address Space
                AddPredefinedNode(SystemContext, mydev1);                               

                // Open connection to DB
                string connectionString = "SERVER=" + DB_server + ";" +
                "DATABASE=" + DB_database + ";" + "UID=" + DB_uid + ";" +
                "PASSWORD=" + DB_password + ";";
                DB_connection = new MySqlConnection(connectionString);
                try
                {
                    DB_connection.Open();
                }
                catch (MySqlException ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                };


                //-------------------------------------------------------------------------------------
                //-------------------------------------------------------------------------------------

                // start periodic method to update my Address Space variables
                m_doprocessingTimer = new Timer(myDoProcessing, null, m_doprocessingInterval, m_doprocessingInterval);
            }
        }
        #endregion

        //Periodic execution method
        private void myDoProcessing(object state)
        {
            try
            {
                lock (Lock)
                {                    
                    //when new piece is made then update DB
                    //format of convertion from real number to string
                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";
                    nfi.NumberGroupSeparator = "";
                    nfi.NumberDecimalDigits = 2;

                    int Piece_SN_Value = (int)Piece_SN.Value;
                    Piece_SN.ClearChangeMasks(SystemContext, false);

                    if ((bool)Piece_Info.Value)
                    {
                        string query = Utils.Format("SELECT Mould_SN, Time FROM db_project.table1 WHERE Piece_SN='{0}';",
                        Piece_SN_Value.ToString("N", nfi));

                        //Create Command
                        MySqlCommand cmd = new MySqlCommand(query, DB_connection);
                        //Create a data reader and Execute the command
                        MySqlDataReader dataReader = cmd.ExecuteReader();

                        //Read the data and store them
                        while (dataReader.Read())
                        {
                            Console.WriteLine(dataReader.GetString(0) + " " + dataReader.GetString(1));
                            Mould_SN.Value = dataReader.GetString(0);
                            Mould_SN.ClearChangeMasks(SystemContext, false);
                            Time.Value = dataReader.GetString(1);
                            Time.ClearChangeMasks(SystemContext, false);
                        }

                        //close Data Reader
                        dataReader.Close();
                    }
                    Piece_Info.ClearChangeMasks(SystemContext, false);
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error doing processing.");
            }          

        }

        #region Private Fields
        private ushort m_namespaceIndex;
        private long m_lastUsedId;

        private Timer m_doprocessingTimer;
        private UInt16 m_doprocessingInterval = 1000;

        private BaseDataVariableState Piece_Info;
        private BaseDataVariableState Piece_SN;
        private BaseDataVariableState Mould_SN;
        private BaseDataVariableState Time;

        //DB
        private MySqlConnection DB_connection;
        private string DB_server = "localhost";
        private string DB_database = "db_project";
        private string DB_uid = "mateusqueiroz";
        private string DB_password = "S2det8dn.";
        #endregion
    }
}
