using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewRemoteDesktop.Server.Network;

namespace NewRemoteDesktop.Server.Services
{
    public class ConnectionService
    {
        // Sử dụng ConcurrentDictionary để đảm bảo an toàn đa luồng (Thread-safe) khi nhiều client kết nối/ngắt kết nối cùng lúc
        private readonly ConcurrentDictionary<string, ClientHandler> _clients = new ConcurrentDictionary<string, ClientHandler>();

        /// <summary>
        /// Thêm một client mới vào danh sách quản lý
        /// </summary>
        public void AddClient(string clientId, ClientHandler client)
        {
            _clients.TryAdd(clientId, client);
        }

        /// <summary>
        /// Xóa client khỏi danh sách khi ngắt kết nối
        /// </summary>
        public void RemoveClient(string clientId)
        {
            _clients.TryRemove(clientId, out _);
        }

        /// <summary>
        /// Lấy ra toàn bộ danh sách các client đang kết nối
        /// </summary>
        public IEnumerable<ClientHandler> GetAllClients()
        {
            return _clients.Values;
        }

        /// <summary>
        /// Tìm kiếm client theo ID
        /// </summary>
        public ClientHandler GetClient(string clientId)
        {
            _clients.TryGetValue(clientId, out var client);
            return client;
        }

        /// <summary>
        /// Đóng tất cả kết nối khi Server dừng hoạt động
        /// </summary>
        public void DisconnectAll()
        {
            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }
            _clients.Clear();
        }
    }
}