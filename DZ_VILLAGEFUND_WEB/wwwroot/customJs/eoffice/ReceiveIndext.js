
/*
 * date 04-01-2023
 * ReceiveIndex
 * setInterval
 */

$("#Organization").select2();
$("#ArchivesType").select2();
$("#Month").select2();
$("#Year").select2();

var Selected = $("#Organization , #ArchivesType, #Month ,#Year");
Selected.change(function () {
    LoadData($("#Organization").val(), $("#ArchivesType").val(), $("#Year").val(), $("#Month").val());
});
$("#Organization").val($("#Organization").val()).change();

setInterval(function () {
    LoadData($("#Organization").val(), $("#ArchivesType").val(), $("#Year").val(), $("#Month").val());
}, 60000);

function LoadData(OrgId, Type, Year, Month) {

    var Data =
    {
        "OrgId": OrgId,
        "Type": Type,
        "Year": Year,
        "Month": Month
    };

    $("#JsonData").html('<img src="/img/loading.gif" width="200">');
    $.get("/EOffice/GetReceives", Data, function (JsonResult) {
        $("#JsonData").html(JsonResult);
        $('#JsonTable').dataTable(
            {
                "pageLength": 50,
                responsive: true,
                lengthChange: false
            });

        // view document
        $("#JsonTable").on("click", ".ViewDetail", function () {
            $.get("/EOffice/ViewDocuments", { "ArchiveId": $(this).attr("data-id") }, function (JsonForm) {
                $("#JsonViewDataArchives").html(JsonForm);
                $("#ViewDataArchives").modal("show");

                $('#use-selector-label span').tooltip();
            });
        });

        //receive and forwork
        $('#JsonTable').on("click", ".forword", function () {
            $.get("/EOffice/ForworkDocument", { "ArchiveId": $(this).attr("data-id"), "OrgId": $("#Organization").val() }, function (JsonResult) {
                $("#JsonForworkArchives").html(JsonResult);
                $("#ViewForworkArchives").modal('show');

                $("#OrgId").select2({
                    dropdownParent: $('#ViewForworkArchives #JsonForm')
                });

                $("#CommandCode").select2({
                    dropdownParent: $('#ViewForworkArchives #JsonForm')
                });

                $('#DestinationOrg').multiselect({
                    buttonWidth: '100%',
                    enableClickableOptGroups: true,
                    enableFiltering: true,
                    maxHeight: 200,
                    numberDisplayed: 1,
                    dropdownParent: $('#ViewForworkArchives #JsonForm')
                });

                $("#IsCirculation").change(function (e) {
                    if (e.target.checked == true) {
                        $("#_IsCirculation").show();
                        $("#show_IsCirculation").hide();
                    }
                    else {
                        $("#_IsCirculation").hide();
                        $("#show_IsCirculation").show();
                    }

                    $("#Circulation").val(e.target.checked);

                });
                $("#IsCirculation").val($("#IsCirculation").val()).change();


                $("#ViewForworkArchives").on("change", "#IsChecked", function (e) {
                    if (e.target.checked == true) {
                        $("#ShowArchiveNumber").show();
                    }
                    else {
                        $("#ShowArchiveNumber").hide();
                    }

                });
                $("#IsChecked").val($("#IsChecked").val()).change();

                // archives number 
                $("#ViewForworkArchives").on("click", "#GetArchiveNumber", function () {
                    $("#ViewForworkArchives").modal('hide');
                    var Data = { "OrgId": $("#Organization").val(), "ArchiveNumber": $(this).attr("data-archiveid"), "ArchiveId": $("#ArchiveId").val() };
                    Swal.fire(
                        {
                            title: "ยืนยันการทำรายการ",
                            text: "คุณต้องการทำรายการนี้ หรือไม่?",
                            type: "warning",
                            showCancelButton: true,
                            confirmButtonText: "ยืนยัน",
                            cancelButtonText: "ยกเลิก",
                            closeOnConfirm: false
                        }).then(function (isConfirm) {
                            if (isConfirm.value == true) {
                                $.get("/EOffice/AddArchiveNumber", Data , function (rs) {
                                    if (rs.valid == true) {
                                        toastr.success(rs.message);
                                        setTimeout(function () {
                                            window.location.href = "";
                                        }, 200);
                                    }
                                    else {
                                        toastr.error('Error!' + rs.message);
                                    }
                                });
                            }
                            else {
                                $("#ViewForworkArchives").modal('show');
                            }
                        });

                });

                // reply
                $("#ViewForworkArchives").on("click", "#reply", function () {
                    $("#ViewForworkArchives").modal("hide");
                    Swal.fire(
                        {
                            title: "ยืนยันการทำรายการ",
                            text: "คุณต้องการทำรายการนี้ หรือไม่?",
                            type: "warning",
                            showCancelButton: true,
                            confirmButtonText: "ยืนยัน",
                            cancelButtonText: "ยกเลิก",
                            closeOnConfirm: false
                        }).then(function (isConfirm) {
                            if (isConfirm.value == true) {
                                var data = {
                                    "ActionName": "reply",
                                    "ArchiveId": $("#ArchiveId").val(),
                                    "Comment": $("#Comment").val()
                                };

                                $.get("/EOffice/UpdateStatusArchive", data, function (rs) {
                                    if (rs.valid == true) {
                                        toastr.success(rs.message);
                                        setTimeout(function () {
                                            window.location.href = "";
                                        }, 200);
                                    }
                                    else {
                                        toastr.error('Error!' + rs.message);
                                    }
                                });
                            }
                            else {
                                $("#ViewForworkArchives").modal("show");
                            }
                        });
                   
                });

                // receive
                $("#ViewForworkArchives").on("click", "#receive", function () {
                    $("#ViewForworkArchives").modal("hide");
                    Swal.fire(
                        {
                            title: "ยืนยันการทำรายการ",
                            text: "คุณต้องการทำรายการนี้ หรือไม่?",
                            type: "warning",
                            showCancelButton: true,
                            confirmButtonText: "ยืนยัน",
                            cancelButtonText: "ยกเลิก",
                            closeOnConfirm: false
                        }).then(function (isConfirm) {
                            if (isConfirm.value == true) {
                                var data = {
                                    "ActionName": "receive",
                                    "ArchiveId": $("#ArchiveId").val(),
                                    "Comment": $("#Comment").val(),
                                    "ReceiveNumber": $("#ReceiveNumber").val(),
                                };

                                $.get("/EOffice/UpdateStatusArchive", data, function (rs) {
                                    if (rs.valid == true) {
                                        toastr.success(rs.message);
                                        setTimeout(function () {
                                            window.location.href = "";
                                        }, 200);
                                    }
                                    else {
                                        toastr.error('Error!' + rs.message);
                                    }
                                });
                            }
                            else {
                                $("#ViewForworkArchives").modal("show");
                            }
                        });
                    
                });

                // form forword
                $('#JsonForm').bootstrapValidator();
                $("#ViewForworkArchives #Submit").click(function () {
                    $('#JsonForm').data("bootstrapValidator").validate();
                    if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                        var Data = new FormData($("#JsonForm")[0]);
                        $.ajax(
                            {
                                type: "POST",
                                url: "/EOffice/ForworkDocument",
                                contentType: false,
                                processData: false,
                                data: Data,
                                success: function (result) {
                                    if (result.valid == true) {
                                        toastr.success(result.message);
                                        setTimeout(function () {
                                            window.location.href = "";
                                        }, 700);
                                    }
                                    else {
                                        toastr.error('Error! ' + result.message);
                                    }
                                }
                            });
                    }
                });

                // save document and finish
                $("#ViewForworkArchives").on("click", "#forword", function () {
                    $("#ViewForworkArchives").modal("hide");
                    Swal.fire(
                        {
                            title: "ยืนยันการทำรายการ",
                            text: "คุณต้องการทำรายการนี้ หรือไม่?",
                            type: "warning",
                            showCancelButton: true,
                            confirmButtonText: "ยืนยัน",
                            cancelButtonText: "ยกเลิก",
                            closeOnConfirm: false
                        }).then(function (isConfirm) {
                            if (isConfirm.value == true) {
                                var data = {
                                    "ArchiveId": $("#ArchiveId").val(),
                                    "Comment": $("#Comment").val()
                                };

                                $.get("/EOffice/UpdateFinishDocument", data, function (rs) {
                                    if (rs.valid == true) {
                                        toastr.success(rs.message);
                                        setTimeout(function () {
                                            window.location.href = "";
                                        }, 200);
                                    }
                                    else {
                                        toastr.error('Error!' + rs.message);
                                    }
                                });
                            }
                            else {
                                $("#ViewForworkArchives").modal("show");
                            }
                        });

                });
            });
        });
    });
}

