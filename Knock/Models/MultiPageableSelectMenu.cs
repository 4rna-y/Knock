using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Models
{
    /// <summary>
    /// Model for implementing selectmenu with multiple item page.
    /// </summary>
    public class MultiPageableSelectMenu
    {
        public int Page { get; set; }
        public List<SelectMenuItem> Items { get; set; }

        public MultiPageableSelectMenu(List<SelectMenuItem> items) 
        {
            this.Items = items;
        }

        public List<SelectMenuItem> GetCurrentSegmentedItems()
        {
            List<SelectMenuItem> dst = new List<SelectMenuItem>();

            for (int i = 0; i < 25 && i + (Page * 25) < Items.Count; i++)
            {
                dst.Add(Items[i + (Page * 25)]);
            }

            return dst;
        }

        public List<SelectMenuItem> GetPreviousSegmentedItems()
        {
            if (Page != 0) Page--;
            return GetCurrentSegmentedItems();
        }

        public List<SelectMenuItem> GetNextSegmentedItems()
        {
            Page++;
            return GetCurrentSegmentedItems();
        }
    }
}
