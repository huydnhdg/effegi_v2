//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApplication.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Agent_Log_Payment
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public int Amount { get; set; }
        public int Status { get; set; }
        public Nullable<System.DateTime> Paydate { get; set; }
    }
}