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
using EasyModbus;

namespace Opc.Ua.Di
{
    public class myIMMControlServerNodeManager : Quickstarts.myIMMControlServer.EmptyNodeManager
    {
        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public myIMMControlServerNodeManager(
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

            //auxiliary variable list to store my variables
            m_dynamicNodes = new List<BaseDataVariableState>();
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

                //create myDevice1/myVar1
                BaseDataVariableState myvar1 = CreateVariable(mydev1, "myVar1", "myVar1", Opc.Ua.DataTypeIds.Double, ValueRanks.Scalar);

                //create myDevice1/Actual_State
                BaseDataVariableState Actual_State = CreateVariable(mydev1, "Actual_State", "Actual_State", Opc.Ua.DataTypeIds.Double, ValueRanks.Scalar);

                //create myDevice1/Sensors_Data
                Sensors_Data = CreateVariable(mydev1, "Sensors_Data", "Sensors_Data", Opc.Ua.DataTypeIds.Double, ValueRanks.OneDimension);

                //create myDevice1/myVar2
                myvar2 = CreateVariable(mydev1, "myVar2", "myVar2", Opc.Ua.DataTypeIds.Int16, ValueRanks.Scalar);

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Poject

                //create myDevice1/Start_Piece
                Start_Piece = CreateVariable(mydev1, "Start_Piece", "Start_Piece", Opc.Ua.DataTypeIds.Boolean, ValueRanks.Scalar);

                // create myDevice1/Mould_1
                Mould_1 = CreateVariable(mydev1, "Mould_1", "Mould_1", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                // create myDevice1/Mould_1_SN
                Mould_1_SN = CreateVariable(mydev1, "Mould_1_SN", "Mould_1_SN", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                // create myDevice1/Mould_2
                Mould_2 = CreateVariable(mydev1, "Mould_2", "Mould_2", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                // create myDevice1/Mould_2_SN
                Mould_2_SN = CreateVariable(mydev1, "Mould_2_SN", "Mould_2_SN", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                // create myDevice1/Piece_SN
                Piece_SN = CreateVariable(mydev1, "Piece_SN", "Piece_SN", Opc.Ua.DataTypeIds.Int32, ValueRanks.Scalar);

                // create myDevice1/Piece_SN
                Machine_SN = CreateVariable(mydev1, "Machine_SN", "Machine_SN", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                //create myDevice1/Change_Mould
                Change_Mould = CreateVariable(mydev1, "Change_Mould", "Change_Mould", Opc.Ua.DataTypeIds.Boolean, ValueRanks.Scalar);

                //create myDevice1/Mould_Changed
                Mould_Changed = CreateVariable(mydev1, "Mould_Changed", "Mould_Changed", Opc.Ua.DataTypeIds.Boolean, ValueRanks.Scalar);

                // create myDevice1/Actual_Mould
                Actual_Mould = CreateVariable(mydev1, "Actual_Mould", "Actual_Mould", Opc.Ua.DataTypeIds.Int32, ValueRanks.Scalar);

                // create myDevice1/Mould_tobeused
                Mould_tobeused = CreateVariable(mydev1, "Mould_tobeused", "Mould_tobeused", Opc.Ua.DataTypeIds.String, ValueRanks.Scalar);

                // create myDevice1/Operator_ID
                Operator_ID = CreateVariable(mydev1, "Operator_ID", "Operator_ID", Opc.Ua.DataTypeIds.Int32, ValueRanks.Scalar);

                // create myDevice1/Robot_Finished
                Robot_Finished = CreateVariable(mydev1, "Robot_Finished", "Robot_Finished", Opc.Ua.DataTypeIds.Boolean, ValueRanks.Scalar);

                //initialize var
                myvar1.Value = (double)0.0;
                myvar2.Value = (short)0;
                Actual_State.Value = (double)0.0;
                Start_Piece.Value = (bool)false;
                Mould_1.Value = (string)"Mould 1";
                Mould_2.Value = (string)"Mould 2";
                Mould_1_SN.Value = (string)"1";
                Mould_2_SN.Value = (string)"2";
                Piece_SN.Value = (int)1;
                Machine_SN.Value = (string)"1";
                Change_Mould.Value = (bool)false;
                Robot_Finished.Value = (bool)true;
                Actual_Mould.Value = (int)0;
                //Sensors_Data.Value = (int[])null;
                Mould_tobeused.Value = Mould_1_SN.Value;
                Mould_Changed.Value = (bool)false;

                //store variable in my variable list for latter access (not necessary if variable is declare as a field of the class)
                m_dynamicNodes.Add(myvar1);
                m_dynamicNodes.Add(Actual_State);
                //add to Address Space
                AddPredefinedNode(SystemContext, mydev1);

                //-------------------------------------------------------------------------------------

                //Create a new object using the type "Device" defined by the DI specification
                //The class DeviceState is defined in the code generated by ModelCompiler                
                myTemperatureSensor = new DeviceState(null);
                myTemperatureSensor.Create(SystemContext,
                        null,
                        new QualifiedName("myTemperatureSensor", m_namespaceIndex),
                        null,
                        true);
                
                //Object is created under DeviceSet
                DeviceSetobj.AddChild(myTemperatureSensor);
                
                //create a variable to hold the value of the temperature, of the type AnalogItem, under myTemperatureSensor
                TemperatureValue = new AnalogItemState(null);
                TemperatureValue.Create(SystemContext,
                   null,
                   new QualifiedName("Temperature", m_namespaceIndex),
                   null,
                   true);
                
                //Object is created under myTemperatureSensor
                myTemperatureSensor.AddChild(TemperatureValue);
                
                //initialize var
                TemperatureValue.Value = (double)0.0;
                
                //the type "Device" has a set of predefined info variables
                myTemperatureSensor.SerialNumber.Value = "AABB112233";
                myTemperatureSensor.Manufacturer.Value = "ACME";
                myTemperatureSensor.Model.Value = new LocalizedText("PT", "MODEL 1.0");
                
                //add to Address Space
                AddPredefinedNode(SystemContext, myTemperatureSensor);

               
                //-------------------------------------------------------------------------------------
                //-------------------------------------------------------------------------------------

                //initialize Modbus client
                Modbus_client = new ModbusClient(Modbus_IP, Modbus_port);
                Modbus_client.ConnectionTimeout = 5000;

                // start periodic method to update my Address Space variables
                m_doprocessingTimer = new Timer(myDoProcessing, null, m_doprocessingInterval, m_doprocessingInterval);
            }
        }

        private LocalizedText Localizedtext(string v1, string v2)
        {
            throw new NotImplementedException();
        }

        private LocalizedText LocalizedText(string v1, string v2)
        {
            throw new NotImplementedException();
        }
        #endregion



        //Periodic execution method
        private void myDoProcessing(object state)
        {
            try
            {
                lock (Lock)
                {
                    if (!Modbus_client.Connected)
                    {
                        //if not connected then connect to modbus server
                        Modbus_client.Connect();
                    }
                    else
                    {
                        //Update variables
                        //Read INT
                        m_dynamicNodes[0].Value = (double)siemens_register2short(Modbus_client.ReadHoldingRegisters(0, 1));
                        m_dynamicNodes[0].ClearChangeMasks(SystemContext, false);
                        //Read REAL
                        //Single = float = single precision = REAL in siemens
                        TemperatureValue.Value = (Single)siemens_registers2single(Modbus_client.ReadHoldingRegisters(1, 2));
                        TemperatureValue.ClearChangeMasks(SystemContext, false);
                        //Write REAL
                        //Single writeval = (Single)TemperatureValue.Value * (Single)2.0;
                        short writeval = (short)myvar2.Value;
                        Modbus_client.WriteSingleRegister(3, (writeval));

                        Modbus_client.WriteSingleRegister(9, (int)Actual_Mould.Value);

                        short writeval2;
                        //Read Sensors Data
                        double[] data = new double[56];
                        int i, j;
                        j = 50;
                        if ((bool)Start_Piece.Value)
                        {
                            writeval2 = 1;
                            for (i = 0; i < 56; i++)
                            {
                                //Register M1100
                                data[i] = (double)siemens_register2short(Modbus_client.ReadHoldingRegisters(j, 1));
                                j++;
                            }
                        }
                        else
                        {
                            writeval2 = 0;
                        }
                        Modbus_client.WriteSingleRegister(6, writeval2);
                        Sensors_Data.Value = (double[])data;
                        Sensors_Data.ClearChangeMasks(SystemContext, false);

                        //Read Actual_State
                        m_dynamicNodes[1].Value = (double)siemens_register2short(Modbus_client.ReadHoldingRegisters(7, 1));
                        m_dynamicNodes[1].ClearChangeMasks(SystemContext, false);
                    }
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


        private List<BaseDataVariableState> m_dynamicNodes;

        private AnalogItemState TemperatureValue;       
        private DeviceState myTemperatureSensor;

        //IP address of the Modbus server (i.e., IP address of the PLC)
        private string Modbus_IP = "192.168.228.203";
        //TCP port of the Modbus server (default: 502)
        private int Modbus_port = 502;
        private ModbusClient Modbus_client;
        private BaseDataVariableState Sensors_Data;
        BaseDataVariableState myvar2;
        BaseDataVariableState Start_Piece;
        private BaseDataVariableState Mould_1_SN;
        private BaseDataVariableState Mould_2;
        private BaseDataVariableState Mould_1;
        private BaseDataVariableState Mould_2_SN;
        private BaseDataVariableState Piece_SN;
        private BaseDataVariableState Machine_SN;
        private BaseDataVariableState Change_Mould;
        private BaseDataVariableState Mould_Changed;
        private BaseDataVariableState Actual_Mould;
        private BaseDataVariableState Mould_tobeused;
        private BaseDataVariableState Operator_ID;
        private BaseDataVariableState Robot_Finished;
        #endregion

        //converts two registers (big-endian) to a single precision float (4bytes)
        private float siemens_registers2single(int[] regs)
        {
            byte[] bytes0 = BitConverter.GetBytes(regs[0]);
            byte[] bytes1 = BitConverter.GetBytes(regs[1]);
            //adapt to host endianess
            if (BitConverter.IsLittleEndian)
            {
                byte[] s = { bytes1[0], bytes1[1], bytes0[0], bytes0[1] };
                return BitConverter.ToSingle(s, 0);
            }
            else
            {
                byte[] s = { bytes0[0], bytes0[1], bytes1[0], bytes1[1] };
                return BitConverter.ToSingle(s, 0);
            }
        }
        //converts a single precision float (4bytes) to two registers (big-endian)
        private int[] siemens_single2registers(float flt)
        {
            byte[] bytes = BitConverter.GetBytes(flt);
            //adapt to host endianess
            if (BitConverter.IsLittleEndian)
            {
                byte[] reg0 = { bytes[2], bytes[3] };
                byte[] reg1 = { bytes[0], bytes[1] };
                int[] ret = { BitConverter.ToInt16(reg0, 0), BitConverter.ToInt16(reg1, 0) };
                return ret;
            }
            else
            {
                byte[] reg0 = { bytes[0], bytes[1] };
                byte[] reg1 = { bytes[2], bytes[3] };
                int[] ret = { BitConverter.ToInt16(reg0, 0), BitConverter.ToInt16(reg1, 0) };
                return ret;
            }
        }

        //converts 1 register (big-endian) to a short int (2bytes)
        private short siemens_register2short(int[] reg)
        {
            byte[] bytes = BitConverter.GetBytes(reg[0]);
            //adapt to host endianess
            if (BitConverter.IsLittleEndian)
            {
                byte[] s = { bytes[0], bytes[1] };
                return BitConverter.ToInt16(s, 0);
            }
            else
            {
                byte[] s = { bytes[1], bytes[0] };
                return BitConverter.ToInt16(s, 0);
            }
        }
    }
}
