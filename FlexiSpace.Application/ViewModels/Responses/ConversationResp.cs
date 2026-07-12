using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ConversationResp
    {
        public string? Id { get; set; } 
        public string? LessorId { get; set; }
        public string? LesseeId { get; set; }
        public DateTime LastMessage { get; set; }

    }
}
