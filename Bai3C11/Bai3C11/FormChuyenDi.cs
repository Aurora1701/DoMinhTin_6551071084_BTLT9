using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Bai3C11.Data;
using Bai3C11.Models;
using Serilog;

namespace Bai3C11;

record TpItem(string Value, string Text);

sealed class TripRow
{
    public int MaChuyen { get; set; }
    public string TenChuyen { get; set; } = "";
    public string DiaDiem { get; set; } = "";
    public DateTime NgayDi { get; set; }
    public int SoNguoi { get; set; }
    public decimal NganSach { get; set; }
    public string PhuongTien { get; set; } = "";      // đã dịch để hiển thị
    public bool DaHoanThanh { get; set; }
    public string PhuongTienGoc { get; set; } = "";   // giá trị lưu trong DB (ẩn)
}

public class FormChuyenDi : Form
{
    static readonly string[] PT = { "Xe máy", "Ô tô", "Xe khách", "Tàu hỏa" };
    int _ma;
    bool _init = true;
    readonly List<(Control c, string k)> _tr = new();

    readonly TextBox txtTen = new(), txtDiaDiem = new(), txtTim = new();
    readonly DateTimePicker dtpNgay = new() { Format = DateTimePickerFormat.Custom };
    readonly DateTimePicker dtpTu = new() { Format = DateTimePickerFormat.Custom };
    readonly DateTimePicker dtpDen = new() { Format = DateTimePickerFormat.Custom };
    readonly NumericUpDown nudNguoi = new() { Minimum = 0, Maximum = 1000, Value = 1 };
    readonly NumericUpDown nudNS = new() { Maximum = 10_000_000_000, Increment = 100000, ThousandsSeparator = true };
    readonly ComboBox cboPT = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Text", ValueMember = "Value" };
    readonly ComboBox cboLang = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly CheckBox chkHT = new(), chkChuaHT = new(), chkLocNgay = new();
    readonly Button btnXem = new();
    readonly ProgressBar prg = new() { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Visible = false };
    readonly GroupBox gInfo = new(), gW = new();
    readonly Label lblTitle = new() { Font = new Font("Segoe UI", 15, FontStyle.Bold), AutoSize = true };
    readonly Label lblIcon = new() { Font = new Font("Segoe UI Emoji", 44), AutoSize = false };
    readonly Label lblDesc = new() { Font = new Font("Segoe UI", 15), AutoSize = false };
    readonly Label lblTemp = new() { AutoSize = false }, lblRain = new() { AutoSize = false };
    readonly Panel pnlWarn = new() { BackColor = Color.FromArgb(255, 224, 178), Visible = false };
    readonly Label lblWarn = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
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

    // trạng thái ô thời tiết (để vẽ lại khi đổi ngôn ngữ)
    WResult? _w; DateOnly _wDate; string _wPlace = ""; string? _wMsgKey;

    T Reg<T>(T c, string key) where T : Control { _tr.Add((c, key)); return c; }
    Label Lb(string key, int x, int y) => Reg(new Label { Left = x, Top = y + 4, AutoSize = true }, key);
    static T At<T>(T c, int x, int y, int w, int h = 26) where T : Control { c.SetBounds(x, y, w, h); return c; }
    Button Btn(string key, int x, int y, EventHandler h, int w = 80)
    { var b = Reg(new Button { Bounds = new Rectangle(x, y, w, 30) }, key); b.Click += h; return b; }

    static QL_DaNgoaiDbContext NewDb() =>
        new(new DbContextOptionsBuilder<QL_DaNgoaiDbContext>().UseSqlServer(Program.Settings.ConnectionStrings.Default).Options);

    public FormChuyenDi()
    {
        ClientSize = new Size(960, 650); StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 9.5f);
        Reg(lblTitle, "title").Location = new Point(10, 8);
        Controls.Add(Lb("lang", 700, 12)); cboLang.Items.AddRange(new object[] { "Tiếng Việt", "English" });
        Controls.Add(At(cboLang, 790, 10, 150));

        Reg(gInfo, "info"); gInfo.Bounds = new Rectangle(10, 45, 435, 235);
        gInfo.Controls.AddRange(new Control[]
        {
            Lb("tenChuyen", 12, 25), At(txtTen, 110, 25, 310),
            Lb("diaDiem", 12, 55), At(txtDiaDiem, 110, 55, 310),
            Lb("ngayDi", 12, 85), At(dtpNgay, 110, 85, 130),
            Lb("soNguoi", 12, 115), At(nudNguoi, 85, 115, 75),
            Lb("phuongTien", 175, 115), At(cboPT, 265, 115, 155),
            Lb("nganSach", 12, 145), At(nudNS, 110, 145, 130),
            At(Reg(chkHT, "daHT"), 110, 175, 200),
            Btn("them", 12, 198, Them, 90), Btn("sua", 110, 198, Sua, 90),
            Btn("xoa", 208, 198, Xoa, 90), Btn("lamMoi", 306, 198, LamMoi, 100)
        });
        Controls.Add(gInfo);

        btnXem = Btn("xemTT", 455, 48, XemThoiTiet, 120); btnXem.Width = 120; Controls.Add(btnXem);
        Controls.Add(At(prg, 585, 53, 355, 20));
        gW.Bounds = new Rectangle(455, 85, 485, 195); gW.Font = new Font("Segoe UI", 9.5f);
        lblIcon.Bounds = new Rectangle(10, 30, 120, 100);
        lblDesc.Bounds = new Rectangle(135, 35, 340, 36);
        lblTemp.Bounds = new Rectangle(137, 78, 330, 22); lblRain.Bounds = new Rectangle(137, 102, 330, 22);
        pnlWarn.Bounds = new Rectangle(12, 140, 460, 40); pnlWarn.Controls.Add(lblWarn);
        gW.Controls.AddRange(new Control[] { lblIcon, lblDesc, lblTemp, lblRain, pnlWarn });
        Controls.Add(gW);

        // bộ lọc
        Controls.AddRange(new Control[]
        {
            At(txtTim, 10, 290, 250), At(Reg(chkChuaHT, "chiChuaHT"), 270, 290, 280),
            At(Reg(chkLocNgay, "locNgay"), 10, 322, 130),
            Lb("tu", 150, 322), At(dtpTu, 190, 322, 120), Lb("den", 325, 322), At(dtpDen, 365, 322, 120),
            Btn("tim", 500, 320, (_, _) => LoadGrid(), 90)
        });
        dtpTu.Value = DateTime.Today; dtpDen.Value = DateTime.Today.AddDays(30);

        dgv.SetBounds(10, 360, 940, 280);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.CellClick += Dgv_CellClick;
        Controls.Add(dgv);

        cboLang.SelectedIndex = L.Lang == "en" ? 1 : 0;
        cboLang.SelectedIndexChanged += (_, _) =>
        {
            L.Lang = cboLang.SelectedIndex == 1 ? "en" : "vi";
            Program.User.Language = L.Lang; Program.User.Save();
            ApplyLanguage();
        };
        txtTim.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadGrid(); } };
        chkChuaHT.CheckedChanged += (_, _) => LoadGrid();

        ApplyLanguage();
        _init = false;
    }

    // ================= Đa ngôn ngữ =================
    void ApplyLanguage()
    {
        var ci = L.Culture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = ci;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = ci;

        foreach (var (c, k) in _tr) c.Text = L.T(k);
        Text = L.T("title");
        txtTim.PlaceholderText = L.T("timPH");
        foreach (var d in new[] { dtpNgay, dtpTu, dtpDen }) d.CustomFormat = ci.DateTimeFormat.ShortDatePattern;

        // Cập nhật lại format số mà không gọi UpdateEditText()
        nudNS.Value = nudNS.Value;
        nudNguoi.Value = nudNguoi.Value;

        var sel = cboPT.SelectedValue as string ?? "Ô tô";
        cboPT.DataSource = PT.Select(p => new TpItem(p, L.T("tp_" + p))).ToList();
        cboPT.SelectedValue = sel;

        LoadGrid();
        RenderWeather();
    }

    // ================= Lưới / lọc =================
    static string TpText(string p) { var t = L.T("tp_" + p); return t.StartsWith("tp_") ? p : t; }

    void LoadGrid()
    {
        try
        {
            using var db = NewDb();
            var q = db.Set<ChuyenDi>().AsNoTracking().AsQueryable();
            var kw = txtTim.Text.Trim();
            if (kw != "") q = q.Where(x => x.TenChuyen.Contains(kw) || x.DiaDiem.Contains(kw));
            if (chkChuaHT.Checked) q = q.Where(x => !x.DaHoanThanh);
            if (chkLocNgay.Checked)
            {
                var tu = DateOnly.FromDateTime(dtpTu.Value); var den = DateOnly.FromDateTime(dtpDen.Value);
                q = q.Where(x => x.NgayDi >= tu && x.NgayDi <= den);
            }
            var rows = q.OrderBy(x => x.NgayDi).ToList().Select(x => new TripRow
            {
                MaChuyen = x.MaChuyen,
                TenChuyen = x.TenChuyen,
                DiaDiem = x.DiaDiem,
                NgayDi = x.NgayDi.ToDateTime(TimeOnly.MinValue),
                SoNguoi = x.SoNguoi,
                NganSach = x.NganSach,
                PhuongTienGoc = x.PhuongTien,
                PhuongTien = TpText(x.PhuongTien),
                DaHoanThanh = x.DaHoanThanh
            }).ToList();
            dgv.DataSource = rows;

            dgv.Columns["PhuongTienGoc"]!.Visible = false;
            var h = new Dictionary<string, string>
            {
                ["MaChuyen"] = "ma",
                ["TenChuyen"] = "tenChuyen",
                ["DiaDiem"] = "diaDiem",
                ["NgayDi"] = "ngayDi",
                ["SoNguoi"] = "soNguoi",
                ["NganSach"] = "nganSach",
                ["PhuongTien"] = "phuongTien",
                ["DaHoanThanh"] = "hoanThanh"
            };
            foreach (var kv in h) dgv.Columns[kv.Key]!.HeaderText = L.T(kv.Value);
            dgv.Columns["NgayDi"]!.DefaultCellStyle.Format = "d";
            dgv.Columns["NganSach"]!.DefaultCellStyle.Format = "N0";
            dgv.Columns["MaChuyen"]!.FillWeight = 30; dgv.Columns["SoNguoi"]!.FillWeight = 50; dgv.Columns["DaHoanThanh"]!.FillWeight = 60;
            foreach (DataGridViewColumn c in dgv.Columns) c.DefaultCellStyle.FormatProvider = L.Culture;
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void Dgv_CellClick(object? s, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgv.Rows[e.RowIndex].DataBoundItem is not TripRow r) return;
        _ma = r.MaChuyen; txtTen.Text = r.TenChuyen; txtDiaDiem.Text = r.DiaDiem;
        dtpNgay.Value = r.NgayDi; nudNguoi.Value = Math.Clamp(r.SoNguoi, 0, 1000);
        nudNS.Value = Math.Min(r.NganSach, nudNS.Maximum);
        cboPT.SelectedValue = r.PhuongTienGoc; chkHT.Checked = r.DaHoanThanh;
    }

    // ================= CRUD =================
    bool HopLe(bool them)
    {
        if (string.IsNullOrWhiteSpace(txtTen.Text) || string.IsNullOrWhiteSpace(txtDiaDiem.Text)) return Loi("errTen");
        if (them && dtpNgay.Value.Date < DateTime.Today) return Loi("errNgay");
        if (nudNguoi.Value < 1) return Loi("errNguoi");
        if (nudNS.Value < 0) return Loi("errNS");
        return true;
    }
    static bool Loi(string key) { MessageBox.Show(L.T(key), L.T("errTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }

    void Doc(ChuyenDi c)
    {
        c.TenChuyen = txtTen.Text.Trim(); c.DiaDiem = txtDiaDiem.Text.Trim();
        c.NgayDi = DateOnly.FromDateTime(dtpNgay.Value); c.SoNguoi = (int)nudNguoi.Value; c.NganSach = nudNS.Value;
        c.PhuongTien = (string)cboPT.SelectedValue!; c.DaHoanThanh = chkHT.Checked;
    }

    void Them(object? s, EventArgs e)
    {
        if (!HopLe(true)) return;
        try
        {
            using var db = NewDb(); var c = new ChuyenDi(); Doc(c); db.Add(c); db.SaveChanges();
            Log.Information("Thêm chuyến đi MaChuyen={Ma}", c.MaChuyen); LamMoi(null, EventArgs.Empty);
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void Sua(object? s, EventArgs e)
    {
        if (_ma == 0) { MessageBox.Show(L.T("chon")); return; }
        if (!HopLe(false)) return;
        try
        {
            using var db = NewDb(); var c = db.Set<ChuyenDi>().Find(_ma);
            if (c != null) { Doc(c); db.SaveChanges(); Log.Information("Sửa chuyến đi MaChuyen={Ma}", _ma); }
            LamMoi(null, EventArgs.Empty);
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void Xoa(object? s, EventArgs e)
    {
        if (_ma == 0) { MessageBox.Show(L.T("chon")); return; }
        if (MessageBox.Show(L.T("confirmDel"), L.T("confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = NewDb(); var c = db.Set<ChuyenDi>().Find(_ma);
            if (c != null) { db.Remove(c); db.SaveChanges(); Log.Information("Xóa chuyến đi MaChuyen={Ma}", _ma); }
            LamMoi(null, EventArgs.Empty);
        }
        catch (Exception ex) { BaoLoiDb(ex); }
    }

    void LamMoi(object? s, EventArgs e)
    {
        _ma = 0; txtTen.Clear(); txtDiaDiem.Clear(); dtpNgay.Value = DateTime.Today; nudNguoi.Value = 1; nudNS.Value = 0;
        cboPT.SelectedIndex = 0; chkHT.Checked = false; txtTim.Clear(); chkChuaHT.Checked = false; chkLocNgay.Checked = false;
        LoadGrid();
    }

    void BaoLoiDb(Exception ex)
    {
        Log.Error(ex, "Lỗi truy cập CSDL");
        MessageBox.Show(L.T("errDb"), L.T("err"), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ================= Thời tiết =================
    async void XemThoiTiet(object? s, EventArgs e)
    {
        var place = txtDiaDiem.Text.Trim();
        if (place == "") { MessageBox.Show(L.T("needPlace")); return; }
        var date = DateOnly.FromDateTime(dtpNgay.Value);

        btnXem.Enabled = false; prg.Visible = true;
        _w = null; _wMsgKey = "loading"; RenderWeather();
        try
        {
            var r = await WeatherService.GetAsync(place, date);
            _wPlace = place; _wDate = date;
            switch (r.Status)
            {
                case WStatus.Ok: _w = r; _wMsgKey = null; break;
                case WStatus.PlaceNotFound: _wMsgKey = "notFound"; break;
                default: _wMsgKey = "noData"; break;
            }
        }
        catch (TaskCanceledException ex) { Log.Warning(ex, "Timeout khi gọi API thời tiết"); _wMsgKey = "timeout"; }
        catch (HttpRequestException ex) { Log.Warning(ex, "Lỗi mạng khi gọi API thời tiết"); _wMsgKey = "netErr"; }
        catch (JsonException ex) { Log.Warning(ex, "Dữ liệu thời tiết không đọc được"); _wMsgKey = "netErr"; }
        finally { btnXem.Enabled = true; prg.Visible = false; }
        RenderWeather();
    }
    void RenderWeather()
    {
        pnlWarn.Visible = false;
        lblIcon.Text = "";
        lblTemp.Text = "";
        lblRain.Text = "";
        gW.Text = "";

        if (_w == null)
        {
            lblDesc.AutoSize = true;
            lblDesc.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            lblDesc.ForeColor = Color.DarkSlateGray;
            lblDesc.Location = new Point(30, 45); // Đặt dịch sang phải, hiển thị trọn vẹn
            lblDesc.Text = _wMsgKey == null ? "" : L.T(_wMsgKey);
            lblDesc.BringToFront();
            return;
        }

        gW.Text = L.T("dubao", _wDate.ToString("d", L.Culture), _wPlace);

        lblDesc.AutoSize = false;
        lblDesc.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
        lblDesc.ForeColor = Color.Black;
        lblDesc.Bounds = new Rectangle(115, 28, 350, 35);

        var (icon, key) = _w.Code switch
        {
            0 => ("☀️", "w0"),
            >= 1 and <= 3 => ("⛅", "w1"),
            45 or 48 => ("🌫️", "w2"),
            (>= 51 and <= 67) or (>= 80 and <= 82) => ("🌧️", "w3"),
            >= 95 and <= 99 => ("⛈️", "w4"),
            >= 71 and <= 77 => ("❄️", "w5"),
            _ => ("🌡️", "wX")
        };

        lblIcon.Text = icon;
        lblDesc.Text = L.T(key);
        lblTemp.Text = L.T("nhiet", Math.Round(_w.Min), Math.Round(_w.Max));
        lblRain.Text = L.T("mua", _w.Rain);

        if (_w.Rain >= 60)
        {
            lblWarn.Text = "⚠  " + L.T("khuyen");
            pnlWarn.Visible = true;
        }

        lblDesc.BringToFront();
        lblTemp.BringToFront();
        lblRain.BringToFront();
        pnlWarn.BringToFront();
    }
}