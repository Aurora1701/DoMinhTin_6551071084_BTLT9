using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Bai2C11.Data;
using Bai2C11.Models;
using Serilog;

namespace Bai2C11;

record RoomItem(int MaPhong, string Text);

/// <summary>Dòng hiển thị trên lưới (kết quả JOIN KhachThue - PhongTro).</summary>
sealed class Row
{
    public int MaKhach { get; set; }
    public string HoTen { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public decimal GiaThue { get; set; }
    public string QueQuan { get; set; } = "";
    public bool TamTru { get; set; }
    // cột ẩn, dùng để đổ lại lên form
    public string GioiTinh { get; set; } = "";
    public DateOnly NgaySinh { get; set; }
    public int MaPhong { get; set; }
    public int MaTinh { get; set; }
    public string TenTinh { get; set; } = "";
    public int MaPhuongXa { get; set; }
    public string TenPhuongXa { get; set; } = "";
}

public class FormKhachThue : Form
{
    const int MaxNguoi = 2;   // quy tắc: mỗi phòng tối đa 2 người -> phòng "hợp lệ" là phòng chưa đủ người
    int _maKhach;
    bool _online, _busy;
    int _wardVer;
    string? _pendingWard;

    readonly TextBox txtTen = new() { MaxLength = 100 };
    readonly RadioButton rdoNam = new() { Text = "Nam", Checked = true, AutoSize = true };
    readonly RadioButton rdoNu = new() { Text = "Nữ", AutoSize = true };
    readonly DateTimePicker dtpNgaySinh = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = new DateTime(2000, 1, 1) };
    readonly TextBox txtSdt = new() { MaxLength = 15 };
    readonly ComboBox cboPhong = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Text", ValueMember = "MaPhong" };
    readonly ComboBox cboTinh = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "name", ValueMember = "code" };
    readonly ComboBox cboPhuong = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "name", ValueMember = "code" };
    readonly CheckBox chkTamTru = new() { Text = "Đã đăng ký tạm trú", AutoSize = true };

    readonly TextBox txtTim = new() { PlaceholderText = "Tìm theo tên hoặc SĐT" };
    readonly ComboBox cboLocPhong = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Text", ValueMember = "MaPhong" };
    readonly ComboBox cboLocTinh = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly Button btnLoi = new() { Text = "Gây lỗi thử", Visible = false };
    readonly Label lblStatus = new() { Dock = DockStyle.Bottom, Height = 24, BorderStyle = BorderStyle.FixedSingle, TextAlign = ContentAlignment.MiddleLeft };
    readonly DataGridView dgv = new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    static Label Lb(string t, int x, int y) => new() { Text = t, Left = x, Top = y + 4, AutoSize = true };
    static T At<T>(T c, int x, int y, int w, int h = 26) where T : Control { c.SetBounds(x, y, w, h); return c; }
    static Button Btn(string t, int x, int y, EventHandler h, int w = 90)
    { var b = new Button { Text = t, Bounds = new Rectangle(x, y, w, 30) }; b.Click += h; return b; }

    static QL_NhaTroDbContext NewDb() =>
        new(new DbContextOptionsBuilder<QL_NhaTroDbContext>().UseSqlServer(Program.Settings.ConnectionStrings.Default).Options);

    public FormKhachThue()
    {
        Text = "Quản lý nhà trọ - Khách thuê";
        ClientSize = new Size(1060, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        KeyPreview = true;

        var gb = new GroupBox { Text = "Giới tính", Bounds = new Rectangle(120, 48, 260, 46) };
        rdoNam.Location = new Point(15, 18); rdoNu.Location = new Point(110, 18);
        gb.Controls.AddRange(new Control[] { rdoNam, rdoNu });

        Controls.AddRange(new Control[]
        {
            Lb("Họ tên", 15, 14), At(txtTen, 120, 14, 260), gb,
            Lb("Ngày sinh", 15, 104), At(dtpNgaySinh, 120, 104, 260),
            Lb("Số điện thoại", 15, 140), At(txtSdt, 120, 140, 260),
            Lb("Phòng", 15, 176), At(cboPhong, 120, 176, 260),
            Lb("Tỉnh/Thành", 15, 212), At(cboTinh, 120, 212, 260),
            Lb("Phường/Xã", 15, 248), At(cboPhuong, 120, 248, 260),
            At(chkTamTru, 120, 284, 200),
            Btn("Thêm", 15, 330, Them), Btn("Sửa", 115, 330, Sua), Btn("Xóa", 215, 330, Xoa), Btn("Làm mới", 315, 330, LamMoi),
            At(btnLoi, 15, 380, 120, 30),
            At(txtTim, 410, 10, 190), At(cboLocPhong, 610, 10, 110), At(cboLocTinh, 725, 10, 150),
            Btn("Tìm", 885, 8, (_, _) => LoadGrid(), 70), Btn("Bỏ lọc", 960, 8, BoLoc, 80),
            lblStatus
        });
        dgv.SetBounds(410, 48, 640, 470);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.CellClick += Dgv_CellClick;
        Controls.Add(dgv);

        cboTinh.SelectedIndexChanged += CboTinh_Changed;
        btnLoi.Click += (_, _) => throw new InvalidOperationException("Lỗi thử nghiệm do người dùng bấm nút 'Gây lỗi thử'");
        KeyDown += (_, e) => { if (e.Control && e.Shift && e.KeyCode == Keys.L) btnLoi.Visible = !btnLoi.Visible; };
        Load += async (_, _) => { NapPhongVaLoc(); LoadGrid(); await LoadProvincesAsync(); };
    }

    // =============== API Tỉnh / Phường-Xã ===============
    async Task LoadProvincesAsync()
    {
        DatDangTai(cboTinh);
        try
        {
            var list = await ProvinceService.GetProvincesAsync();
            _busy = true; cboTinh.DataSource = list; cboTinh.SelectedIndex = -1; _busy = false;
            cboTinh.Enabled = true; _online = true;
            lblStatus.Text = $"Đã tải {list.Count} tỉnh/thành từ provinces.open-api.vn";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            Log.Warning(ex, "Không tải được danh sách tỉnh/thành từ API");
            _online = false;
            ChoGoTay(cboTinh); ChoGoTay(cboPhuong);
            lblStatus.Text = "Chế độ nhập tay Tỉnh/Phường-Xã (không có mạng)";
            MessageBox.Show("Không tải được danh sách địa chỉ, vui lòng kiểm tra mạng.\nBạn vẫn có thể gõ tay Tỉnh/Phường-Xã để lưu khách.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    void CboTinh_Changed(object? s, EventArgs e)
    {
        if (_busy || !_online) return;
        if (cboTinh.SelectedItem is Area a && a.code > 0) _ = LoadWardsAsync(a.code);
    }

    async Task LoadWardsAsync(int tinh)
    {
        int ver = ++_wardVer;
        DatDangTai(cboPhuong);
        try
        {
            var w = await ProvinceService.GetWardsAsync(tinh);
            if (ver != _wardVer) return;
            cboPhuong.DataSource = w; cboPhuong.SelectedIndex = -1; cboPhuong.Enabled = true;
            if (_pendingWard != null)
            {
                int i = cboPhuong.FindStringExact(_pendingWard);
                if (i >= 0) cboPhuong.SelectedIndex = i;
                _pendingWard = null;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (ver != _wardVer) return;
            Log.Warning(ex, "Không tải được danh sách phường/xã của tỉnh {Tinh}", tinh);
            ChoGoTay(cboPhuong);
            MessageBox.Show("Không tải được danh sách địa chỉ, vui lòng kiểm tra mạng.\nBạn có thể gõ tay Phường/Xã.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    void DatDangTai(ComboBox c)
    {
        c.DropDownStyle = ComboBoxStyle.DropDownList;
        _busy = true;
        c.DataSource = new List<Area> { new() { name = "Đang tải…", code = 0 } };
        _busy = false; c.Enabled = false;
    }

    void ChoGoTay(ComboBox c)
    {
        _busy = true; c.DataSource = null; c.Items.Clear(); _busy = false;
        c.DropDownStyle = ComboBoxStyle.DropDown; c.Enabled = true; c.Text = "";
    }

    // =============== Phòng ===============
    List<RoomItem> LayPhong(QL_NhaTroDbContext db, bool chiPhongConCho, int? giuLai = null)
    {
        var dem = db.Set<KhachThue>().GroupBy(k => k.MaPhong).Select(g => new { g.Key, N = g.Count() })
                    .ToDictionary(x => x.Key, x => x.N);
        return db.Set<PhongTro>().AsNoTracking().OrderBy(p => p.TenPhong).AsEnumerable()
            .Where(p => !chiPhongConCho || p.MaPhong == giuLai || dem.GetValueOrDefault(p.MaPhong) < MaxNguoi)
            .Select(p => new RoomItem(p.MaPhong, $"{p.TenPhong} - {p.GiaThue:N0} đ")).ToList();
    }

    void NapPhongVaLoc(int? giuLai = null)
    {
        try
        {
            using var db = NewDb();
            cboPhong.DataSource = LayPhong(db, true, giuLai);
            var all = LayPhong(db, false); all.Insert(0, new RoomItem(0, "Tất cả"));
            var cur = cboLocPhong.SelectedValue as int?;
            cboLocPhong.DataSource = all; cboLocPhong.SelectedValue = cur ?? 0;
            var tinh = db.Set<KhachThue>().Select(k => k.TenTinh).Distinct().OrderBy(t => t).ToList();
            var curT = cboLocTinh.SelectedItem as string;
            cboLocTinh.Items.Clear(); cboLocTinh.Items.Add("Tất cả"); cboLocTinh.Items.AddRange(tinh.ToArray());
            cboLocTinh.SelectedItem = curT ?? "Tất cả"; if (cboLocTinh.SelectedIndex < 0) cboLocTinh.SelectedIndex = 0;
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    // =============== Lưới (JOIN) + tìm kiếm ===============
    void LoadGrid()
    {
        try
        {
            using var db = NewDb();
            var q = from k in db.Set<KhachThue>().AsNoTracking()
                    join p in db.Set<PhongTro>() on k.MaPhong equals p.MaPhong
                    select new Row
                    {
                        MaKhach = k.MaKhach,
                        HoTen = k.HoTen,
                        SoDienThoai = k.SoDienThoai,
                        TenPhong = p.TenPhong,
                        GiaThue = p.GiaThue,
                        QueQuan = k.TenPhuongXa + " - " + k.TenTinh,
                        TamTru = k.DaDangKyTamTru,
                        GioiTinh = k.GioiTinh,
                        NgaySinh = k.NgaySinh,
                        MaPhong = k.MaPhong,
                        MaTinh = k.MaTinh,
                        TenTinh = k.TenTinh,
                        MaPhuongXa = k.MaPhuongXa,
                        TenPhuongXa = k.TenPhuongXa
                    };
            var kw = txtTim.Text.Trim();
            if (kw != "") q = q.Where(r => r.HoTen.Contains(kw) || r.SoDienThoai.Contains(kw));
            if (cboLocPhong.SelectedValue is int mp && mp > 0) q = q.Where(r => r.MaPhong == mp);
            if (cboLocTinh.SelectedIndex > 0) { var t = (string)cboLocTinh.SelectedItem!; q = q.Where(r => r.TenTinh == t); }

            dgv.DataSource = q.OrderBy(r => r.TenPhong).ThenBy(r => r.HoTen).ToList();
            var an = new[] { "MaKhach", "GioiTinh", "NgaySinh", "MaPhong", "MaTinh", "TenTinh", "MaPhuongXa", "TenPhuongXa" };
            foreach (var n in an) dgv.Columns[n]!.Visible = false;
            var h = new Dictionary<string, string> { ["HoTen"] = "Họ tên", ["SoDienThoai"] = "SĐT", ["TenPhong"] = "Phòng", ["GiaThue"] = "Giá thuê", ["QueQuan"] = "Quê quán", ["TamTru"] = "Tạm trú" };
            foreach (var kv in h) dgv.Columns[kv.Key]!.HeaderText = kv.Value;
            dgv.Columns["GiaThue"]!.DefaultCellStyle.Format = "N0";
            dgv.Columns["TenPhong"]!.FillWeight = 40; dgv.Columns["TamTru"]!.FillWeight = 40;
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void BoLoc(object? s, EventArgs e)
    {
        txtTim.Clear(); cboLocPhong.SelectedValue = 0; cboLocTinh.SelectedIndex = 0; LoadGrid();
    }

    // =============== Chọn dòng ===============
    async void Dgv_CellClick(object? s, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgv.Rows[e.RowIndex].DataBoundItem is not Row r) return;
        _maKhach = r.MaKhach;
        txtTen.Text = r.HoTen; rdoNam.Checked = r.GioiTinh == "Nam"; rdoNu.Checked = r.GioiTinh != "Nam";
        dtpNgaySinh.Value = r.NgaySinh.ToDateTime(TimeOnly.MinValue);
        txtSdt.Text = r.SoDienThoai; chkTamTru.Checked = r.TamTru;
        NapPhongVaLoc(r.MaPhong); cboPhong.SelectedValue = r.MaPhong;

        if (_online)
        {
            _pendingWard = r.TenPhuongXa;
            if (cboTinh.SelectedValue is int cur && cur == r.MaTinh) await LoadWardsAsync(r.MaTinh);
            else cboTinh.SelectedValue = r.MaTinh;
        }
        else { cboTinh.Text = r.TenTinh; cboPhuong.Text = r.TenPhuongXa; }
    }

    // =============== Kiểm tra + đọc dữ liệu ===============
    bool HopLe()
    {
        if (string.IsNullOrWhiteSpace(txtTen.Text)) return Loi("Họ tên không được để trống.", txtTen);
        if (dtpNgaySinh.Value.Date >= DateTime.Today) return Loi("Ngày sinh không hợp lệ.", dtpNgaySinh);
        var sdt = txtSdt.Text.Trim();
        if (sdt.Length < 9 || !sdt.All(char.IsDigit)) return Loi("Số điện thoại phải gồm 9-15 chữ số.", txtSdt);
        if (cboPhong.SelectedValue is not int) return Loi("Hãy chọn phòng (chỉ liệt kê phòng còn chỗ).", cboPhong);
        if (!cboPhuong.Enabled || !cboTinh.Enabled) return Loi("Danh sách địa chỉ đang tải, vui lòng đợi giây lát.", cboTinh);
        if (string.IsNullOrWhiteSpace(TenTinh())) return Loi("Hãy chọn (hoặc gõ) Tỉnh/Thành.", cboTinh);
        if (string.IsNullOrWhiteSpace(TenPhuong())) return Loi("Hãy chọn (hoặc gõ) Phường/Xã.", cboPhuong);
        return true;
    }
    static bool Loi(string m, Control c) { MessageBox.Show(m, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning); c.Focus(); return false; }
    string TenTinh() => cboTinh.SelectedItem is Area a && a.code > 0 ? a.name : cboTinh.Text.Trim();
    string TenPhuong() => cboPhuong.SelectedItem is Area a && a.code > 0 ? a.name : cboPhuong.Text.Trim();
    int MaTinh() => cboTinh.SelectedItem is Area a ? a.code : 0;
    int MaPhuong() => cboPhuong.SelectedItem is Area a ? a.code : 0;

    void Doc(KhachThue k)
    {
        k.HoTen = txtTen.Text.Trim(); k.GioiTinh = rdoNam.Checked ? "Nam" : "Nữ";
        k.NgaySinh = DateOnly.FromDateTime(dtpNgaySinh.Value); k.SoDienThoai = txtSdt.Text.Trim();
        k.MaTinh = MaTinh(); k.TenTinh = TenTinh(); k.MaPhuongXa = MaPhuong(); k.TenPhuongXa = TenPhuong();
        k.DaDangKyTamTru = chkTamTru.Checked; k.MaPhong = (int)cboPhong.SelectedValue!;
    }

    /// <summary>Phòng có khách -> "Đang thuê", không còn ai -> "Trống".</summary>
    static void DongBoPhong(QL_NhaTroDbContext db, int maPhong)
    {
        int n = db.Set<KhachThue>().Count(k => k.MaPhong == maPhong);
        var p = db.Set<PhongTro>().Find(maPhong);
        if (p != null) { p.TinhTrang = n > 0 ? "Đang thuê" : "Trống"; db.SaveChanges(); }
    }

    // =============== CRUD ===============
    void Them(object? s, EventArgs e)
    {
        if (!HopLe()) return;
        try
        {
            using var db = NewDb(); using var tx = db.Database.BeginTransaction();
            var k = new KhachThue(); Doc(k);
            if (db.Set<KhachThue>().Count(x => x.MaPhong == k.MaPhong) >= MaxNguoi)
            { MessageBox.Show("Phòng này đã đủ người."); return; }
            db.Add(k); db.SaveChanges(); DongBoPhong(db, k.MaPhong); tx.Commit();
            Log.Information("Thêm khách MaKhach={Ma} vào phòng {Phong}", k.MaKhach, k.MaPhong);
            LamMoi(null, EventArgs.Empty);
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void Sua(object? s, EventArgs e)
    {
        if (_maKhach == 0) { MessageBox.Show("Hãy bấm chọn một khách trong danh sách để sửa."); return; }
        if (!HopLe()) return;
        try
        {
            using var db = NewDb(); using var tx = db.Database.BeginTransaction();
            var k = db.Set<KhachThue>().Find(_maKhach);
            if (k is null) { MessageBox.Show("Khách này không còn tồn tại."); return; }
            int cu = k.MaPhong; Doc(k);
            if (k.MaPhong != cu && db.Set<KhachThue>().Count(x => x.MaPhong == k.MaPhong) >= MaxNguoi)
            { MessageBox.Show("Phòng mới đã đủ người."); return; }
            db.SaveChanges(); DongBoPhong(db, k.MaPhong); if (cu != k.MaPhong) DongBoPhong(db, cu);
            tx.Commit();
            Log.Information("Sửa khách MaKhach={Ma}", k.MaKhach);
            LamMoi(null, EventArgs.Empty);
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void Xoa(object? s, EventArgs e)
    {
        if (_maKhach == 0) { MessageBox.Show("Hãy bấm chọn một khách trong danh sách để xóa."); return; }
        if (MessageBox.Show("Bạn có chắc muốn xóa khách này?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = NewDb(); using var tx = db.Database.BeginTransaction();
            var k = db.Set<KhachThue>().Find(_maKhach);
            if (k != null) { int p = k.MaPhong; db.Remove(k); db.SaveChanges(); DongBoPhong(db, p); }
            tx.Commit();
            Log.Information("Xóa khách MaKhach={Ma}", _maKhach);
            LamMoi(null, EventArgs.Empty);
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void LamMoi(object? s, EventArgs e)
    {
        _maKhach = 0; _pendingWard = null;
        txtTen.Clear(); rdoNam.Checked = true; dtpNgaySinh.Value = new DateTime(2000, 1, 1);
        txtSdt.Clear(); chkTamTru.Checked = false;
        cboTinh.SelectedIndex = -1;
        if (_online) { _busy = true; cboPhuong.DataSource = null; _busy = false; } else { cboTinh.Text = ""; cboPhuong.Text = ""; }
        NapPhongVaLoc(); LoadGrid(); txtTen.Focus();
    }

    void BaoLoiDb(Exception ex)
    {
        Log.Error(ex, "Lỗi truy cập CSDL");
        MessageBox.Show("Không truy cập được cơ sở dữ liệu. Vui lòng kiểm tra kết nối rồi thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}