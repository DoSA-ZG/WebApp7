﻿tableLangSettings = {
  search: "Pretraga",
  info: "_START_ - _END_ od ukupno _TOTAL_ zapisa",
  lengthMenu: "Prikaži _MENU_ zapisa",
  paginate: {
      first: "Prva",
      previous: "Prethodna",
      next: "Sljedeća",
      last: "Zadnja"
  },
  emptyTable: "Nema podataka za prikaz",
  infoEmpty: "Nema podataka za prikaz",
  infoFiltered: "(filtrirano od ukupno _MAX_ zapisa)",
  zeroRecords: "Ne postoje traženi podaci"
};

function clearOldMessage() {
$("#tempmessage").siblings().remove();
$("#tempmessage").removeClass("alert-success");
$("#tempmessage").removeClass("alert-danger");
$("#tempmessage").html('');
}

//Pri svakom kliku kontrole koja ima css klasu delete zatraži potvrdu
//za razliku od  //$(".delete").click ovo se odnosi i na elemente koji će se pojaviti u budućnosti 
//dinamičkim učitavanjem
$(function () {
    $(document).on('click', '.delete', function (event) {
        if (!confirm("Obrisati zapis?")) {
            event.preventDefault();
        }
    });
    $(document).on('click', '.delete-partner', function (event) {
        if (!confirm("Obrisati partnera i sve njegove zahtjeve?")) {
            event.preventDefault();
        }
    });
    $(document).on('click', '.edit-partner', function (event) {
        var noviId = $("#novi-partner").val();
        var stariId = $("#stari-partner").val();

        if (noviId != stariId) {
            if (!confirm("Prebaciti sve zahtijeve na novog partnera?")) {
                event.preventDefault();
            }
        }
    });
    $(document).on('click', '.delete-zahtjev', function (event) {
        if (!confirm("Obrisati zahtjev i sve njegove zadatke?")) {
            event.preventDefault();
        }
    });
});