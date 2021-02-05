using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Models
{
    public class DbResponse : IDbResponse
    {
        public DbResponse()
        {
            IsSuccess = false;
            Message = string.Empty;
        }

        public DbResponse(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public DbResponse(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }


        public bool IsSuccess { get; set; }

        public string Message { get; set; }

    }


    public interface IDbResponse
    {
        bool IsSuccess { get; set; }

        string Message { get; set; }
    }
}
