﻿// ============================================================================
// FileName: SIPEventDialog.cs
//
// Description:
// Represents a child level XML element on a SIP event dialog payload that contains the specifics 
// of a participant in a dialog as described in: 
// RFC4235 "An INVITE-Initiated Dialog Event Package for the Session Initiation Protocol (SIP)".
//
// Author(s):
// Aaron Clauson
//
// History:
// 28 Feb 2010	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2010 Aaron Clauson (aaron@sipsorcery.com), SIPSorcery Ltd, London, UK (www.sipsorcery.com)
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SIPSorcery.Sys;

namespace SIPSorcery.SIP
{
    public class SIPEventDialogParticipant
    {
        private static readonly string m_dialogXMLNS = SIPEventConsts.DIALOG_XML_NAMESPACE_URN;
        private static readonly string m_sipsorceryXMLNS = SIPEventConsts.SIPSORCERY_DIALOG_XML_NAMESPACE_URN;
        private static readonly string m_includeSDPFilter = SIPEventFilters.SIP_DIALOG_INCLUDE_SDP;

        public string DisplayName;
        public SIPURI URI;
        public SIPURI TargetURI;
        public int CSeq;
        public string SDP;              // The Session Description Payload from the dialog participant.

        private SIPEventDialogParticipant()
        { }

        public SIPEventDialogParticipant(string displayName, SIPURI uri, SIPURI targetURI, int cseq, string sdp)
        {
            DisplayName = displayName;
            URI = uri;
            TargetURI = targetURI;
            CSeq = cseq;
            SDP = sdp;
        }

        public static SIPEventDialogParticipant Parse(string participantXMLStr)
        {
            XElement dialogElement = XElement.Parse(participantXMLStr);
            return Parse(dialogElement);
        }

        public static SIPEventDialogParticipant Parse(XElement participantElement)
        {
            XNamespace ns = m_dialogXMLNS;
            XNamespace ss = m_sipsorceryXMLNS;
            SIPEventDialogParticipant participant = new SIPEventDialogParticipant();

            XElement identityElement = participantElement.Element(ns + "identity");
            if (identityElement != null)
            {
                participant.DisplayName = (identityElement.Attribute("display-name") != null) ? identityElement.Attribute("display-name").Value : null;
                participant.URI = SIPURI.ParseSIPURI(identityElement.Value);
            }

            XElement targetElement = participantElement.Element(ns + "target");
            if (targetElement != null)
            {
                participant.TargetURI = SIPURI.ParseSIPURI(targetElement.Attribute("uri").Value);
            }

            participant.CSeq = (participantElement.Element(ns + "cseq") != null) ? Convert.ToInt32(participantElement.Element(ns + "cseq").Value) : 0;

            participant.SDP = (participantElement.Element(ss + "sdp") != null) ? participantElement.Element(ss + "sdp").Value.Replace("\n", "\r\n") : null;

            return participant;
        }

        /// <summary>
        /// Puts the dialog participant information to an XML element.
        /// </summary>
        /// <param name="nodeName">A participant can represent a local or remote party, the node name needs to be set to either "local" or "remote".</param>
        /// <returns>An XML element representing the dialog participant.</returns>
        public XElement ToXML(string nodeName, string filter)
        {
            bool includeSDP = (!filter.IsNullOrBlank() && Regex.Match(filter, m_includeSDPFilter).Success) ? true : false;

            XNamespace ns = m_dialogXMLNS;
            XNamespace ss = m_sipsorceryXMLNS;
            XElement participantElement = new XElement(ns + nodeName);

            if(URI != null)
            {
                XElement identityElement = new XElement(ns + "identity", URI.ToString());
                if(!DisplayName.IsNullOrBlank())
                {
                    identityElement.Add(new XAttribute("display-name", DisplayName));
                }
                participantElement.Add(identityElement);
            }

            if(TargetURI != null)
            {
                XElement targetElement = new XElement(ns + "target", new XAttribute("uri", TargetURI.ToString()));
                participantElement.Add(targetElement);
            }

            if(CSeq > 0)
            {
                XElement cseqElement = new XElement(ns + "cseq", CSeq);
                participantElement.Add(cseqElement);
            }

            if (includeSDP && !SDP.IsNullOrBlank())
            {
                XElement sdpElement = new XElement(ss + "sdp", SDP);
                participantElement.Add(sdpElement);
            }

            return participantElement;
        }
    }
}
