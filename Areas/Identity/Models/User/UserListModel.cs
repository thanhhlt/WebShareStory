#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.UserViewModels
{
        public class UserListModel
        {
            public int totalUsers { get; set; }
            public int countPages { get; set; }

            public int ITEMS_PER_PAGE { get; set; } = 10;

            public int currentPage { get; set; }

            public string SearchString { get; set; }

            public string MessageSearchResult { get; set; }

            public List<UserIndex> users { get; set; }
        }

        public class UserIndex
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            [DataType(DataType.Date)]
            [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
            public DateTime AccountCreated { get; set; }
        }
}