using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Contract : BaseEntity
    {
        public long Id { get; set; }
        //nguoi cho thue
        public string LessorId { get; set; }
        //nguoi thue
        public string LesseeId { get; set; }
        public long SpaceId { get; set; }
        public long PrimaryBookingRequestId { get; set; }
        // so cmnd nguoi cho thue
        public string LessorNumberCard { get; set; }
        // so cmnd nguoi thue
        public string LesseeNumberCard { get; set; }
        public string Description { get; set; }
        //dien tich
        public decimal Acreage { get; set; }
        //thoi gian thue tinh theo don vi ngay, thang, gio
        public int Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        //tien dat coc
        public decimal DepositAmount { get; set; }
        public decimal Price { get; set; }
        public ContractStatusEnum Status { get; set; }

        public virtual Space Space { get; set; }
        public virtual PrimaryBookingRequest PrimaryBookingRequest { get; set; }
    }
}
