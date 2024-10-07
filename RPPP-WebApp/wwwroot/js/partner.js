$(document).on('click', '.deleterow', function (event) {
    event.preventDefault();
    var tr = $(this).parents("tr");
    tr.remove();
    clearOldMessage();
});

$("#zahtijev-dodaj").click(function (event) {
    event.preventDefault();
    dodajZahtijev();
    getAutocomplete();
});

function dodajZahtijev() {
    var id = parseInt($("#zahtijev-id").val());
    $("#zahtijev-id").val(id - 1);

    var opisZahtijev = $("#opis-zahtijev").val();
    var prioritet = $("#prioritet").val();
    var vrstaZahtijev = $("#vrsta-zahtijev").val();

    var add = true;
    if (opisZahtijev == "") {
        add = false;
        var opisError = "<li>Opis zahtijeva ne smije biti prazan!</li>";
    }
    if (prioritet == "") {
        add = false;
        var prioritetError = "<li>Prioritet mora biti postavljen!</li>";
    }
    if (vrstaZahtijev == "") {
        add = false;
        var vrstaZahtijevError = "<li>Naziv vrste zahtjeva ne smije biti prazan!</li>";
    }

    if (!add) {
        document.getElementById('validation-message').innerHTML = "<ul>" + opisError + prioritetError + vrstaZahtijevError + "</ul>";
    } else {
        var template = $('#template').html();
        document.getElementById('validation-message').innerHTML = "";

        template = template.replace(/--opis-zahtijev--/g, opisZahtijev)
            .replace(/--prioritet--/g, prioritet)
            .replace(/--vrsta-zahtijev--/g, vrstaZahtijev)
            .replace(/--id--/g, id)
        $(template).find('tr').insertBefore($("#table-zahtijevi").find('tr').last());

        $("#opis-zahtijev").val('');
        $("#prioritet").val('');
        $("#vrsta-zahtijev").val('');
    }
}