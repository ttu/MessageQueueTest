﻿using NetMQ;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.NetMQ
{
    public class Pull<TData>
    {
        private List<string> _bindEndPoints;
        private Action<TData> _process;

        public Pull(string endpoint, Action<TData> process)
        {
            _bindEndPoints = new List<string> { endpoint };
            _process = process;
        }

        public void Start()
        {
            using (var context = NetMQContext.Create())
            {
                using (var socket = context.CreatePullSocket())
                {
                    foreach (var bindEndPoint in _bindEndPoints)
                        socket.Connect(bindEndPoint);

                    socket.ReceiveReady += _socket_ReceiveReady;

                    var poller = new Poller();
                    poller.AddSocket(socket);
                    poller.Start();
                }
            }
        }

        private async void _socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMessage();

            // Response socket has index 1, Dealer 2
            var request = SerializationMethods.FromByteArray<TData>(msg[1].Buffer);

            await DoWork(request);
        }

        private async Task DoWork(TData request)
        {
            await Task.Factory.StartNew(() =>
            {
                _process(request);
            });
        }
    }
}