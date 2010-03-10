// ============================================================================
// FileName: SIPMonitorMachineEvent.cs
//
// Description:
// Describes monitoring events that are for machine notifications to initiate actions such as
// updating a user interface. The events will not typically contain useful information for a human
// viewer.
//
// Author(s):
// Aaron Clauson
//
// History:
// 14 Nov 2008	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2008 Aaron Clauson (aaronc@blueface.ie), Blue Face Ltd, Dublin, Ireland (www.blueface.ie)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that 
// the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
// disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of Blue Face Ltd. 
// nor the names of its contributors may be used to endorse or promote products derived from this software without specific 
// prior written permission. 
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
// BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
// OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, 
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
// ============================================================================

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using SIPSorcery.Sys;
using log4net;

#if UNITTEST
using NUnit.Framework;
#endif

namespace SIPSorcery.SIP.App
{
    /// <summary>
    /// Describes monitoring events that are for machine notifications to initiate actions such as
    /// updating a user interface. The events will not typically contain useful information for a human
    /// viewer.
    /// </summary>
    public class SIPMonitorMachineEvent : SIPMonitorEvent
    {
        public const string SERIALISATION_PREFIX = "2";             // Prefix appended to the front of a serialised event to identify the type. 

        public SIPMonitorMachineEventTypesEnum MachineEventType;
        public SIPDialogue Dialogue;

        private SIPMonitorMachineEvent()
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;
            ClientType = SIPMonitorClientTypesEnum.Machine;
        }

        public SIPMonitorMachineEvent(SIPMonitorMachineEventTypesEnum machineEventType, string owner, SIPEndPoint remoteEndPoint, string message)
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;

            RemoteEndPoint = remoteEndPoint;
            ClientType = SIPMonitorClientTypesEnum.Machine;
            Username = owner;
            MachineEventType = machineEventType;
            Message = message;
        }

        public SIPMonitorMachineEvent(SIPMonitorMachineEventTypesEnum machineEventType, string owner, SIPDialogue sipEventDialogue)
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;

            ClientType = SIPMonitorClientTypesEnum.Machine;
            Username = owner;
            MachineEventType = machineEventType;
            Dialogue = sipEventDialogue;
        }

        public static SIPMonitorMachineEvent ParseMachineEventCSV(string eventCSV)
        {
            try
            {
                SIPMonitorMachineEvent machineEvent = new SIPMonitorMachineEvent();

                if (eventCSV.IndexOf(END_MESSAGE_DELIMITER) != -1)
                {
                    eventCSV.Remove(eventCSV.Length - 2, 2);
                }

                string[] eventFields = eventCSV.Split(new char[] { '|' });

                machineEvent.MachineEventType = SIPMonitorMachineEventTypes.GetMonitorMachineTypeForId(Convert.ToInt32(eventFields[1]));
                machineEvent.Created = DateTimeOffset.Parse(eventFields[2]);
                machineEvent.Username = eventFields[3];
                machineEvent.RemoteEndPoint = SIPEndPoint.ParseSIPEndPoint(eventFields[4]);
                machineEvent.Message = eventFields[5];
                string dialogueXML = eventFields[6].Trim('#');

                if (!dialogueXML.IsNullOrBlank())
                {
                    XDocument dialogueElement = XDocument.Parse(dialogueXML);
                    SIPDialogueAsset dialogueAsset = new SIPDialogueAsset();
                    dialogueAsset.Load(dialogueElement.Root);
                    machineEvent.Dialogue = dialogueAsset.SIPDialogue;
                }

                return machineEvent;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPMonitorMachineEvent ParseEventCSV. " + excp.Message);
                return null;
            }
        }

        public override string ToCSV()
        {
            try
            {
                int machineEventTypeId = (int)MachineEventType;
                string remoteSocket = (RemoteEndPoint != null) ? RemoteEndPoint.ToString() : null;
                string dialogueXML = (Dialogue != null) ? (new SIPDialogueAsset(Dialogue)).ToXML() : null;

                string csvEvent =
                    SERIALISATION_PREFIX + "|" +
                    machineEventTypeId + "|" +
                    Created.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + "|" +
                    Username + "|" +
                    remoteSocket + "|" +
                    Message + "|" +
                    dialogueXML
                    + END_MESSAGE_DELIMITER;

                return csvEvent;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPMonitorMachineEvent ToCSV. " + excp.Message);
                return null;
            }
        }

        public string ToAnonymousCSV()
        {
            try
            {
                int machineEventTypeId = (int)MachineEventType;
                string remoteSocket = null;

                if (RemoteEndPoint != null)
                {
                    // This is the equivalent of applying a /20 mask to the IP address to obscure the bottom 12 bits of the address.
                    byte[] addressBytes = RemoteEndPoint.SocketEndPoint.Address.GetAddressBytes();
                    addressBytes[3] = 0;
                    addressBytes[2] = (byte)(addressBytes[2] & 0xf0);
                    IPAddress anonymisedIPAddress = new IPAddress(addressBytes);
                    remoteSocket = (RemoteEndPoint != null) ? anonymisedIPAddress.ToString() + ":" + RemoteEndPoint.SocketEndPoint.Port : null;
                }

                string csvEvent =
                    SERIALISATION_PREFIX + "|" +
                    machineEventTypeId + "|" +
                     Created.ToString() + "|" +
                    "|" +
                    remoteSocket + "|" +
                    "|" +
                    END_MESSAGE_DELIMITER;

                return csvEvent;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPMonitorMachineEvent ToAnonymousCSV. " + excp.Message);
                return null;
            }
        }

        #region Unit testing.

#if UNITTEST
	
		[TestFixture]
		public class SIPMonitorMachineEventUnitTest
		{
			[TestFixtureSetUp]
			public void Init()
			{
				
			}

		
			[TestFixtureTearDown]
			public void Dispose()
			{			
				
			}
		}

#endif

        #endregion
    }
}
