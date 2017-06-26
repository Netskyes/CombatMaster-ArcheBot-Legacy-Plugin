using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CombatMaster.UI
{
    public partial class ImageView : Form
    {
        public void SetImage(Image image) => pbox_Image.Image = image;


        public ImageView()
        {
            InitializeComponent();
        }

        private void pbox_Image_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
