$(document).on('click', '.deleterow', function (event) {
    event.preventDefault();
    var tr = $(this).parents("tr");
    tr.remove();
    clearOldMessage();
});

$("#zadatak-dodaj").click(function (event) {
    event.preventDefault();
    dodajZadatak();
    getAutocomplete();
});

function dodajZadatak() {
    var id = parseInt($("#zadatak-id").val());
    $("#zadatak-id").val(id - 1);

    var status = $("#status").val();
    var trajanje = $("#trajanje").val();
    var vrstaZadatak = $("#vrsta-zadatak").val();

    var template = $('#template').html();

    template = template.replace(/--status--/g, status)
        .replace(/--trajanje--/g, trajanje)
        .replace(/--vrsta-zadatak--/g, vrstaZadatak)
        .replace(/--id--/g, id)
    $(template).find('tr').insertBefore($("#table-zadaci").find('tr').last());

    $("#status").val('');
    $("#trajanje").val('');
    $("#vrsta-zadatak").val('');
}