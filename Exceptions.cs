using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUKA.RSI.Sensors.Exceptions {
    public class GetErrorCountLimitException : Exception {
        public GetErrorCountLimitException() {
        }

        public GetErrorCountLimitException(string? message) : base(message) {
        }

        public GetErrorCountLimitException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
    public class SendErrorCountLimitException : Exception {
        public SendErrorCountLimitException() {
        }

        public SendErrorCountLimitException(string? message) : base(message) {
        }

        public SendErrorCountLimitException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
    public class DifferenceSendAndGetDataException : Exception {
        public TimeSpan DifferenseTime { get; set; }
        public DifferenceSendAndGetDataException() {
        }

        public DifferenceSendAndGetDataException(string? message) : base(message) {
        }

        public DifferenceSendAndGetDataException(string? message, Exception? innerException) : base(message, innerException) {
        }

    }
}
