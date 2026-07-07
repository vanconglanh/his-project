namespace ProDiabHis.Application.Appointments;

/// <summary>Sinh PDF Giay hen tai kham (QuestPDF), dung chung khung thuong hieu diaB.</summary>
public interface IAppointmentSlipPdfBuilder
{
    byte[] Build(AppointmentSlipData data);
}
