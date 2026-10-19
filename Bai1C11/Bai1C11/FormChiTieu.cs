using Microsoft.EntityFrameworkCore;
using Serilog;
using Bai1C11.Data;
using Bai1C11.Models;

namespace Bai1C11;

public class FormChiTieu : Form
{
    static readonly string[] Loai = { "Ăn uống", "Đi lại", "Học tập", "Giải trí", "Khác" };
    int _maCT;   // 0 = chưa chọn dòng nào

    readonly DateTimePicker dtpNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
    readonly TextBox txtNoiDung = new() { MaxLength = 100 };
    readonly ComboBox cboLoai = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly NumericUpDown nudTien = new() { Maximum = 1_000_000_000, Increment = 1000, ThousandsSeparator = true };
    readonly TextBox txtGhiChu = new() { Multiline = true, MaxLength = 200, ScrollBars = ScrollBars.Vertical };

    readonly TextBox txtTim = new() { PlaceholderText = "Nhập nội dung..." };
    readonly ComboBox cboLoc = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly DateTimePicker dtpTu = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
    readonly DateTimePicker dtpDen = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };

    readonly DataGridView dgv = new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
        BackgroundColor = Color.White
    };
    readonly Label lblTong = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        ForeColor = Color.Firebrick,
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right
    };

    static Label Lb(string t, int x, int y) => new() { Text = t, Left = x, Top = y + 4, AutoSize = true };
    static T At<T>(T c, int x, int y, int w, int h = 26) where T : Control { c.SetBounds(x, y, w, h); return c; }
    static Button Btn(string t, int x, int y, EventHandler h)
    {
        var b = new Button { Text = t, Bounds = new Rectangle(x, y, 90, 30) };
        b.Click += h; return b;
    }
    static QL_ChiTieuDbContext NewDb() =>
        new(new DbContextOptionsBuilder<QL_ChiTieuDbContext>().UseSqlServer(Program.Settings.ConnectionStrings.Default).Options);

    public FormChiTieu()
    {
        Text = "Sổ chi tiêu cá nhân";
        ClientSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        var g1 = new GroupBox { Text = "Thông tin khoản chi", Bounds = new Rectangle(10, 8, 530, 190) };
        g1.Controls.AddRange(new Control[]
        {
            Lb("Ngày chi", 15, 28), At(dtpNgay, 100, 28, 150),
            Lb("Nội dung", 15, 62), At(txtNoiDung, 100, 62, 410),
            Lb("Loại chi", 15, 96), At(cboLoai, 100, 96, 150),
            Lb("Số tiền", 270, 96), At(nudTien, 335, 96, 175),
            Lb("Ghi chú", 15, 130), At(txtGhiChu, 100, 130, 410, 48)
        });

        var g2 = new GroupBox { Text = "Tìm kiếm", Bounds = new Rectangle(550, 8, 340, 190) };
        var btnTim = new Button { Text = "Tìm" };
        btnTim.Click += (_, _) => LoadData();
        g2.Controls.AddRange(new Control[]
        {
            Lb("Nội dung", 15, 28), At(txtTim, 85, 28, 235),
            Lb("Loại chi", 15, 62), At(cboLoc, 85, 62, 235),
            Lb("Từ ngày", 15, 96), At(dtpTu, 85, 96, 150),
            Lb("Đến ngày", 15, 130), At(dtpDen, 85, 130, 150), At(btnTim, 245, 128, 75, 30)
        });

        cboLoai.Items.AddRange(Loai);
        cboLoai.SelectedIndex = 0;
        cboLoc.Items.Add("Tất cả"); cboLoc.Items.AddRange(Loai); cboLoc.SelectedIndex = 0;
        var today = DateTime.Today;
        dtpTu.Value = new DateTime(today.Year, today.Month, 1);
        dtpDen.Value = today;

        dgv.SetBounds(10, 245, 880, 300);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.CellClick += Dgv_CellClick;
        lblTong.Location = new Point(650, 560);

        Controls.AddRange(new Control[]
        {
            g1, g2,
            Btn("Thêm", 110, 208, Them), Btn("Sửa", 210, 208, Sua),
            Btn("Xóa", 310, 208, Xoa), Btn("Làm mới", 410, 208, LamMoi),
            dgv, lblTong
        });

        txtTim.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadData(); } };
        cboLoc.SelectedIndexChanged += (_, _) => LoadData();
        Load += (_, _) => LoadData();
    }

    // ---------- Tải dữ liệu theo bộ lọc, cập nhật Label tổng ----------
    void LoadData()
    {
        try
        {
            using var db = NewDb();
            var tu = DateOnly.FromDateTime(dtpTu.Value);
            var den = DateOnly.FromDateTime(dtpDen.Value);
            var q = db.Set<ChiTieu>().AsNoTracking().Where(x => x.NgayChi >= tu && x.NgayChi <= den);
            var kw = txtTim.Text.Trim();
            if (kw != "") q = q.Where(x => x.NoiDung.Contains(kw));
            if (cboLoc.SelectedIndex > 0) { var l = (string)cboLoc.SelectedItem!; q = q.Where(x => x.LoaiChi == l); }

            var list = q.OrderByDescending(x => x.NgayChi).ThenByDescending(x => x.MaCt).ToList();
            dgv.DataSource = list;
            FormatGrid();
            lblTong.Text = $"Tổng chi: {list.Sum(x => x.SoTien):N0} đ";
        }
        catch (Exception ex) { BaoLoiDb(ex, "Tải danh sách chi tiêu"); }
    }

    void FormatGrid()
    {
        var h = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        { ["MaCt"] = "Mã", ["NgayChi"] = "Ngày chi", ["NoiDung"] = "Nội dung", ["LoaiChi"] = "Loại chi", ["SoTien"] = "Số tiền", ["GhiChu"] = "Ghi chú" };
        foreach (DataGridViewColumn c in dgv.Columns) if (h.TryGetValue(c.Name, out var t)) c.HeaderText = t;
        if (dgv.Columns["NgayChi"] is { } c1) c1.DefaultCellStyle.Format = "dd/MM/yyyy";
        if (dgv.Columns["SoTien"] is { } c2) { c2.DefaultCellStyle.Format = "N0"; c2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
        if (dgv.Columns["MaCt"] is { } c3) c3.FillWeight = 40;
    }

    void BaoLoiDb(Exception ex, string viec)
    {
        Log.Error(ex, "Lỗi CSDL khi: {Viec}", viec);
        MessageBox.Show("Không truy cập được cơ sở dữ liệu. Vui lòng kiểm tra kết nối (appsettings.json) rồi thử lại.",
            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ---------- Chọn dòng -> đổ lên control ----------
    void Dgv_CellClick(object? s, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgv.Rows[e.RowIndex].DataBoundItem is not ChiTieu c) return;
        _maCT = c.MaCt;
        dtpNgay.Value = c.NgayChi.ToDateTime(TimeOnly.MinValue);
        txtNoiDung.Text = c.NoiDung;
        cboLoai.SelectedItem = c.LoaiChi; if (cboLoai.SelectedIndex < 0) cboLoai.SelectedIndex = Loai.Length - 1;
        nudTien.Value = Math.Min(c.SoTien, nudTien.Maximum);
        txtGhiChu.Text = c.GhiChu ?? "";
    }

    // ---------- Kiểm tra hợp lệ ----------
    bool HopLe()
    {
        if (string.IsNullOrWhiteSpace(txtNoiDung.Text)) return Loi("Nội dung không được để trống.", txtNoiDung);
        if (nudTien.Value <= 0) return Loi("Số tiền phải lớn hơn 0.", nudTien);
        if (dtpNgay.Value.Date > DateTime.Today) return Loi("Ngày chi không được lớn hơn hôm nay.", dtpNgay);
        if (cboLoai.SelectedItem is null) return Loi("Hãy chọn loại chi.", cboLoai);
        return true;
    }
    static bool Loi(string msg, Control c)
    {
        MessageBox.Show(msg, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        c.Focus(); return false;
    }

    void Doc(ChiTieu e)
    {
        e.NgayChi = DateOnly.FromDateTime(dtpNgay.Value);
        e.NoiDung = txtNoiDung.Text.Trim();
        e.LoaiChi = (string)cboLoai.SelectedItem!;
        e.SoTien = nudTien.Value;
        e.GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim();
    }

    // ---------- CRUD ----------
    void Them(object? s, EventArgs e)
    {
        if (!HopLe()) return;
        try
        {
            using var db = NewDb();
            var ct = new ChiTieu(); Doc(ct);
            db.Add(ct); db.SaveChanges();
            Log.Information("Thêm khoản chi MaCT={MaCT}, số tiền {SoTien}", ct.MaCt, ct.SoTien);
            XoaForm(); LoadData();
        }
        catch (Exception ex) { BaoLoiDb(ex, "Thêm khoản chi"); }
    }

    void Sua(object? s, EventArgs e)
    {
        if (_maCT == 0) { MessageBox.Show("Hãy bấm chọn một dòng trong danh sách để sửa."); return; }
        if (!HopLe()) return;
        try
        {
            using var db = NewDb();
            var ct = db.Set<ChiTieu>().Find(_maCT);
            if (ct is null) { MessageBox.Show("Khoản chi này không còn tồn tại."); LoadData(); return; }
            Doc(ct); db.SaveChanges();
            Log.Information("Sửa khoản chi MaCT={MaCT}", ct.MaCt);
            XoaForm(); LoadData();
        }
        catch (Exception ex) { BaoLoiDb(ex, "Sửa khoản chi"); }
    }

    void Xoa(object? s, EventArgs e)
    {
        if (_maCT == 0) { MessageBox.Show("Hãy bấm chọn một dòng trong danh sách để xóa."); return; }
        if (MessageBox.Show("Bạn có chắc muốn xóa khoản chi này?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = NewDb();
            var ct = db.Set<ChiTieu>().Find(_maCT);
            if (ct != null) { db.Remove(ct); db.SaveChanges(); }
            Log.Information("Xóa khoản chi MaCT={MaCT}", _maCT);
            XoaForm(); LoadData();
        }
        catch (Exception ex) { BaoLoiDb(ex, "Xóa khoản chi"); }
    }

    void LamMoi(object? s, EventArgs e)
    {
        XoaForm();
        txtTim.Clear(); cboLoc.SelectedIndex = 0;
        dtpTu.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dtpDen.Value = DateTime.Today;
        LoadData();
    }

    private void InitializeComponent()
    {

    }

    void XoaForm()
    {
        _maCT = 0; dtpNgay.Value = DateTime.Today; txtNoiDung.Clear();
        cboLoai.SelectedIndex = 0; nudTien.Value = 0; txtGhiChu.Clear(); txtNoiDung.Focus();
    }
}