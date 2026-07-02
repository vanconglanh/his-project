using System;
using System.Collections.Generic;
using FluentAssertions;
using ProDiabHis.Infrastructure.Pharmacy;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// Unit test cho phần ánh xạ thuần <see cref="DtqgPrescriptionPayloadBuilder.MapPayload"/>
/// (không I/O). Phần đọc DB (BuildAsync) verify ở integration test với MySQL thật.
/// </summary>
public class DtqgPrescriptionPayloadBuilderTests
{
    private static DtqgPrescriptionPayloadBuilder.HeaderRow Header(string? enc = "enc-1", string? dxIcd = null) =>
        new() { EncounterId = enc, PatientId = "pat-1", DiagnosisIcd10 = dxIcd, Note = "Uong sau an" };

    private static DtqgPrescriptionPayloadBuilder.PatientRow Patient() =>
        new() { Code = "BN000123", FullName = "Nguyen Van An", Gender = "MALE", DateOfBirth = new DateTime(1968, 3, 12) };

    [Fact]
    public void MapPayload_maps_patient_and_year_of_birth()
    {
        var data = DtqgPrescriptionPayloadBuilder.MapPayload(
            Header(), Patient(), "HN4791234567890",
            new DtqgPrescriptionPayloadBuilder.DiagnosisRow { Icd10Code = "E11", Name = "Dai thao duong type 2" },
            new List<DtqgPrescriptionPayloadBuilder.ItemRow>());

        data.Patient.Code.Should().Be("BN000123");
        data.Patient.FullName.Should().Be("Nguyen Van An");
        data.Patient.Gender.Should().Be("MALE");
        data.Patient.YearOfBirth.Should().Be(1968);
        data.Patient.InsuranceCardNo.Should().Be("HN4791234567890");
        data.Note.Should().Be("Uong sau an");
    }

    [Fact]
    public void MapPayload_numbers_drug_items_sequentially()
    {
        var items = new List<DtqgPrescriptionPayloadBuilder.ItemRow>
        {
            new() { DrugName = "Metformin", GenericName = "Metformin", Strength = "500mg", Unit = "vien", Quantity = 60, Dosage = "1v x2/ngay", DtqgDrugCode = "D001" },
            new() { DrugName = "Gliclazide MR", Strength = "30mg", Unit = "vien", Quantity = 30 },
        };

        var data = DtqgPrescriptionPayloadBuilder.MapPayload(Header(), Patient(), null, null, items);

        data.Drugs.Should().HaveCount(2);
        data.Drugs[0].No.Should().Be(1);
        data.Drugs[0].DrugName.Should().Be("Metformin");
        data.Drugs[0].DtqgDrugCode.Should().Be("D001");
        data.Drugs[0].Quantity.Should().Be(60);
        data.Drugs[1].No.Should().Be(2);
        data.Drugs[1].DrugName.Should().Be("Gliclazide MR");
    }

    [Fact]
    public void MapPayload_uses_primary_diagnosis_when_present()
    {
        var data = DtqgPrescriptionPayloadBuilder.MapPayload(
            Header(dxIcd: "Z00"), Patient(), null,
            new DtqgPrescriptionPayloadBuilder.DiagnosisRow { Icd10Code = "E11", Name = "DTD type 2" },
            new List<DtqgPrescriptionPayloadBuilder.ItemRow>());

        data.DiagnosisIcd10.Should().Be("E11");
        data.DiagnosisName.Should().Be("DTD type 2");
    }

    [Fact]
    public void MapPayload_falls_back_to_header_icd10_when_no_diagnosis_row()
    {
        var data = DtqgPrescriptionPayloadBuilder.MapPayload(
            Header(dxIcd: "E11"), Patient(), null, null,
            new List<DtqgPrescriptionPayloadBuilder.ItemRow>());

        data.DiagnosisIcd10.Should().Be("E11");
        data.DiagnosisName.Should().BeNull();
    }

    [Fact]
    public void MapPayload_handles_empty_drug_list()
    {
        var data = DtqgPrescriptionPayloadBuilder.MapPayload(Header(), Patient(), null, null,
            new List<DtqgPrescriptionPayloadBuilder.ItemRow>());

        data.Drugs.Should().BeEmpty();
    }
}
