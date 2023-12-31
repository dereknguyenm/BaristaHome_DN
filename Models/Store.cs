﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BaristaHome.Models
{
    public class Store
    {
        public int StoreId { get; set; }

        [StringLength(32), Display(Name = "Store Name")]
        public string StoreName { get; set; }

        [StringLength(5), Display(Name = "Store Invitation Code")]
        public string? StoreInviteCode { get; set; }

        // Relationships
        public virtual ICollection<Checklist>? Checklists { get; set; }
        public virtual ICollection<Drink>? Drinks { get; set; }
        public virtual ICollection<Feedback>? Feedbacks { get; set; }
        public virtual ICollection<InventoryItem>? InventoryItems { get; set; }
        public virtual ICollection<Sale>? Sales { get; set; }
        public virtual ICollection<Shift>? Shifts { get; set; }
        public virtual ICollection<StoreTimer>? StoreTimers { get; set; }
        public virtual ICollection<User>? Users { get; set; }
        public virtual ICollection<Announcement>? Announcement { get; set; }

    }
}
