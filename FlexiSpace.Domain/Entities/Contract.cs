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
        public string ConversationId { get; set; }
        public DateOnly Date { get; set; }
        // thong tin nguoi cho thue
        public string LessorNumberCard { get; set; }
        public string LessorName { get; set; }
        public DateOnly LessorCardIssuanceDate { get; set; }
        public string LessorCardAddress { get; set; }

        // thong tin nguoi nguoi thue
        public string LesseeNumberCard { get; set; }
        public string LesseeName { get; set; }
        public DateOnly LesseeCardIssuanceDate { get; set; }
        public string LesseeCardAddress { get; set; }
        public string Description { get; set; }
        //dien tich
        public decimal Acreage { get; set; }
        public DurationUnitEnum DurationUnit { get; set; }
        public int Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        //tien dat coc
        public decimal DepositAmount { get; set; }
        public decimal Price { get; set; }
        public string ContractSnapshot { get; set; }
        public ContractStatusEnum Status { get; set; }
        public virtual Space Space { get; set; }
        public virtual Conversation Conversation { get; set; }
        public virtual User Lessor { get; set; }
        public virtual User Lessee { get; set; }    
        public virtual PrimaryBookingRequest PrimaryBookingRequest { get; set; }
        public virtual ContractVerification ContractVerification { get; set; }
        public virtual ICollection<ContractSchedule> ContractSchedules { get; set; } = new List<ContractSchedule>();
    }
}
