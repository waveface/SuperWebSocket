﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace SuperWebSocket.Protocol
{
    public class DataAsyncReader : AsyncReaderBase
    {
        //-1 means that don't find start pos
        private int m_StartPos = -1;

        public DataAsyncReader(AsyncReaderBase prevReader, int startPos)
        {
            Segments = prevReader.GetLeftBuffer();
            m_StartPos = startPos;
        }

        public DataAsyncReader(AsyncReaderBase prevReader)
        {
            Segments = prevReader.GetLeftBuffer();
        }

        #region ICommandAsyncReader Members

        public override StringCommandInfo FindCommand(SocketContext context, byte[] readBuffer, int offset, int length)
        {
            Segments.AddSegment(new ArraySegment<byte>(readBuffer, offset, length));

            if (m_StartPos < 0)
            {
                m_StartPos = Segments.IndexOf(WebSocketConstant.StartByte);

                if (m_StartPos < 0)
                {
                    //Continue to read following bytes to seek start pos
                    NextCommandReader = new DataAsyncReader(this);
                    return null;
                }
            }

            int endPos = Segments.IndexOf(WebSocketConstant.EndByte, m_StartPos, Segments.Count - m_StartPos);

            if (endPos < 0)
            {
                //Continue to search end byte
                NextCommandReader = new DataAsyncReader(this, m_StartPos);
                return null;
            }

            var commandInfo = new StringCommandInfo(WebSocketConstant.CommandData, Encoding.UTF8.GetString(Segments.ToArrayData(m_StartPos + 1, endPos - m_StartPos)), new string[]{});

            int left = Segments.Count - endPos - 1;

            if (left > 0)
            {
                Segments.ClearSegements();
                Segments.AddSegment(new ArraySegment<byte>(readBuffer, offset + length - left, left));
            }

            NextCommandReader = new DataAsyncReader(this);
            return commandInfo;
        }

        #endregion
    }
}
