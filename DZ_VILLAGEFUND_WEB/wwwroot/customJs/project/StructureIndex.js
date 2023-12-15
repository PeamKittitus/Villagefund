
$(function () {

    $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
    $.get("/ElectronicProjects/GetStructures", function (JsonResult) {
        $("#JsonData").html(JsonResult);
        $('#JsonTable').dataTable(
            {
                order: [[2, 'desc']],
                responsive: true,
                lengthChange: false,
                dom: "<'row mb-3'<'col-sm-12 col-md-6 d-flex align-items-center justify-content-start'f><'col-sm-12 col-md-6 d-flex align-items-center justify-content-end'lB>>" +
                    "<'row'<'col-sm-12'tr>>" +
                    "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
                buttons: [
                    {
                        extend: 'print',
                        text: 'สั่งพิมพ์',
                        titleAttr: 'Print Table',
                        className: 'btn-outline-primary btn-sm'
                    }
                ]
            });

        // edit form
        $('#JsonTable').on("click", ".edit", function () {
            const Id = $(this).attr("data-val");
            $.get("/ElectronicProjects/FormEditStructure", { "Id": Id }, function (JsonForm) {
                $("#FormCreate").html(JsonForm);
                $("#FormCreateModal").modal("show");

                $('#JsonForm').bootstrapValidator();
                $("#Submit").click(function () {
                    $('#JsonForm').data("bootstrapValidator").validate();
                    if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                        var Data = new FormData($("#JsonForm")[0]);
                        $.ajax(
                            {
                                type: "POST",
                                url: "/ElectronicProjects/FormEditOrgStructure",
                                contentType: false,
                                processData: false,
                                data: Data,
                                success: function (result) {
                                    if (result.valid == true) {
                                        toastr.success(result.message);
                                        setTimeout(function () {
                                            window.location.href = "/ElectronicProjects/StructureIndex";
                                        }, 700);
                                    }
                                    else {
                                        toastr.error('Error! ' + result.message);
                                    }
                                }
                            });
                    }
                });
            });
        });

        // delete
        $('#JsonTable').on("click", ".delete", function () {
            const Id = $(this).attr("data-val");
            Swal.fire(
                {
                    title: "ยืนยันการทำรายการ",
                    text: "คุณต้องการลบรายการนี้ หรือไม่?",
                    type: "warning",
                    showCancelButton: true,
                    confirmButtonText: "ยืนยัน",
                    cancelButtonText: "ยกเลิก",
                    closeOnConfirm: false
                }).then(function (isConfirm) {
                    if (isConfirm.value == true) {
                        $.get("/ElectronicProjects/DeleteOrg", { "Id": Id }, function (Result) {
                            if (Result.valid == true) {
                                toastr.success(Result.message);
                                setTimeout(function () {
                                    window.location.href = "/ElectronicProjects/StructureIndex";
                                }, 1000);
                            }
                            else {
                                toastr.error('Error!' + Result.message);
                            }
                        });
                    }
                });
        });
    });

    // create new 
    $("#Create").click(function () {
        $.get("/ElectronicProjects/FormAddStructure", function (JsonForm) {
            $("#FormCreate").html(JsonForm);
            $("#FormCreateModal").modal("show");

            $('#JsonForm').bootstrapValidator();
            $("#Submit").click(function () {
                $('#JsonForm').data("bootstrapValidator").validate();
                if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                    var Data = new FormData($("#JsonForm")[0]);
                    $.ajax(
                        {
                            type: "POST",
                            url: "/ElectronicProjects/FormAddOrgStructure",
                            contentType: false,
                            processData: false,
                            data: Data,
                            success: function (result) {
                                if (result.valid == true) {
                                    toastr.success(result.message);
                                    setTimeout(function () {
                                        window.location.href = "/ElectronicProjects/StructureIndex";
                                    }, 700);
                                }
                                else {
                                    toastr.error('Error! ' + result.message);
                                }
                            }
                        });
                }
            });
        });
    });
});