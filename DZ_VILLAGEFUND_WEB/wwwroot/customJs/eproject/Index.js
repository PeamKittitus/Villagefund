
/*
 * get data
 * init table 
 */

$(function () {
    $("#CurrentBudgetYear").select2();
    $("#CurrentBudgetYear").change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/ElectronicProjects/GetProjects", { "BudgetYear": $(this).val() }, function (JsonResult) {
            $("#JsonData").html(JsonResult);
            $('#JsonTable').dataTable(
                {
                    "pageLength": 50,
                    responsive: true,
                    lengthChange: false
                });

            /* edit data */
            $("#JsonData").on("click", ".edit", function () {
                window.location.href = "/ElectronicProjects/CreateProject?ProjectId=" + $(this).attr("data-val");
            });

            /* delete data */
            $("#JsonData").on("click", ".delete", function () {
                const ProjectId = $(this).attr("data-val");
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
                            $.get("/ElectronicProjects/DeleteProject", { "ProjectId": ProjectId }, function (rs) {
                                if (rs.valid == true) {
                                    toastr.success(rs.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 700);
                                }
                                else {
                                    toastr.error('แจ้งเตือน! ' + rs.message);
                                }
                            });
                        }
                    });
            });
        });
    });
    $("#CurrentBudgetYear").val($("#CurrentBudgetYear").val()).change();
   
})