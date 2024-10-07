using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using RPPP_WebApp.Models; 

namespace RPPP_WebApp.Models
{
    public partial class Rppp07Context : DbContext
    {
        public Rppp07Context(DbContextOptions<Rppp07Context> options)
            : base(options)
        {
        }

        public virtual DbSet<Dokument> Dokumenti { get; set; }

        public virtual DbSet<Kartica> Kartice { get; set; }

        public virtual DbSet<Koordinator> Koordinatori { get; set; }

        public virtual DbSet<Korisnik> Korisnici { get; set; }

        public virtual DbSet<Osoba> Osobe { get; set; }

        public virtual DbSet<Partner> Partneri { get; set; }

        public virtual DbSet<Projekt> Projekti { get; set; }

        public virtual DbSet<Suradnik> Suradnici { get; set; }

        public virtual DbSet<Trajanje> Trajanja { get; set; }

        public virtual DbSet<Transakcija> Transakcije { get; set; }

        public virtual DbSet<Voditelj> Voditelji { get; set; }

        public virtual DbSet<VrstaDoc> VrstaDocsa { get; set; }

        public virtual DbSet<VrstaProjekta> VrstaProjekta { get; set; }

        public virtual DbSet<VrstaSuradnika> VrstaSuradnika { get; set; }

        public virtual DbSet<VrstaTran> VrstaTrans { get; set; }

        public virtual DbSet<VrstaZadatka> VrstaZadatka { get; set; }

        public virtual DbSet<VrstaZahtjeva> VrstaZahtjeva { get; set; }

        public virtual DbSet<Zadatak> Zadaci { get; set; }

        public virtual DbSet<Zahtijev> Zahtjevi { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dokument>(entity =>
            {
                entity.HasKey(e => e.IdDoc).HasName("PK__DOKUMENT__23BDF8AE68C1BBD0");

                entity.ToTable("DOKUMENT");

                entity.HasIndex(e => new { e.IdProjekt, e.NazivDatoteke }, "UQ_DOKUMENT_NAZIV_PROJECTSCOPE").IsUnique();

                entity.Property(e => e.IdDoc).HasColumnName("id_doc");
                entity.Property(e => e.Dokument1).HasColumnName("Dokument");
                entity.Property(e => e.IdProjekt).HasColumnName("ID_projekt");
                entity.Property(e => e.IdVrsta).HasColumnName("id_vrsta");
                entity.Property(e => e.NazivDatoteke)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_datoteke");
                entity.Property(e => e.NazivDokument)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_dokument");

                entity.HasOne(d => d.IdProjektNavigation).WithMany(p => p.Dokumenti)
                    .HasForeignKey(d => d.IdProjekt)
                    .HasConstraintName("FK__DOKUMENT__ID_pro__02FC7413");

                entity.HasOne(d => d.IdVrstaNavigation).WithMany(p => p.Dokuments)
                    .HasForeignKey(d => d.IdVrsta)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DOKUMENT_VRSTA_DOC");
            });

            modelBuilder.Entity<Kartica>(entity =>
            {
                entity.HasKey(e => e.BrRacuna).HasName("PK__KARTICA__089CD7B01CE10A99");

                entity.ToTable("KARTICA");

                entity.Property(e => e.BrRacuna)
                    .ValueGeneratedNever()
                    .HasColumnName("br_racuna");
                entity.Property(e => e.IdProjekt).HasColumnName("ID_projekt");
                entity.Property(e => e.Stanje)
                    .HasColumnType("numeric(10, 2)")
                    .HasColumnName("stanje");

                entity.HasOne(d => d.IdProjektNavigation).WithMany(p => p.Karticas)
                    .HasForeignKey(d => d.IdProjekt)
                    .HasConstraintName("FK__KARTICA__ID_proj__571DF1D5");
            });

            modelBuilder.Entity<Koordinator>(entity =>
            {
                entity.HasKey(e => e.IdSuradnik).HasName("PK__KOORDINA__389BD220E43AA65B");

                entity.ToTable("KOORDINATOR");

                entity.Property(e => e.IdSuradnik)
                    .ValueGeneratedNever()
                    .HasColumnName("ID_suradnik");

                entity.HasOne(d => d.IdSuradnikNavigation).WithOne(p => p.Koordinator)
                    .HasForeignKey<Koordinator>(d => d.IdSuradnik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__KOORDINAT__ID_su__6EF57B66");
            });

            modelBuilder.Entity<Korisnik>(entity =>
            {
                entity.HasKey(e => e.IbanKorisnik).HasName("PK__KORISNIK__A640B79ADD266645");

                entity.ToTable("KORISNIK");

                entity.Property(e => e.IbanKorisnik)
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("IBAN_korisnik");
                entity.Property(e => e.NazivKorisnik)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_korisnik");
            });

            modelBuilder.Entity<Osoba>(entity =>
            {
                entity.HasKey(e => e.IdSuradnik).HasName("PK__OSOBA__389BD2209F485D84");

                entity.ToTable("OSOBA");

                entity.HasIndex(e => e.IbanOsoba, "UQ__OSOBA__409F9A076D405278").IsUnique();

                entity.HasIndex(e => e.BrMob, "UQ__OSOBA__8AD0DC1249E96FFD").IsUnique();

                entity.HasIndex(e => e.Email, "UQ__OSOBA__AB6E6164AFDE8AC5").IsUnique();

                entity.HasIndex(e => e.Oib, "UQ__OSOBA__CB394B3EFF8570C2").IsUnique();

                entity.Property(e => e.IdSuradnik)
                    .ValueGeneratedNever()
                    .HasColumnName("ID_suradnik");
                entity.Property(e => e.BrMob)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("BR_mob");
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("email");
                entity.Property(e => e.IbanOsoba)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("IBAN_osoba");
                entity.Property(e => e.Ime)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("ime");
                entity.Property(e => e.Oib)
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("OIB");
            });

            modelBuilder.Entity<Partner>(entity =>
            {
                entity.HasKey(e => e.IdSuradnik).HasName("PK__PARTNER__389BD22034CF7574");

                entity.ToTable("PARTNER");

                entity.Property(e => e.IdSuradnik)
                    .ValueGeneratedNever()
                    .HasColumnName("ID_suradnik");

                entity.HasOne(d => d.IdSuradnikNavigation).WithOne(p => p.Partner)
                    .HasForeignKey<Partner>(d => d.IdSuradnik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__PARTNER__ID_sura__75A278F5");
            });

            modelBuilder.Entity<Projekt>(entity =>
            {
                entity.HasKey(e => e.IdProjekt).HasName("PK__PROJEKT__1231144D8D7586E9");

                entity.ToTable("PROJEKT");

                entity.HasIndex(e => e.NazivProjekt, "UQ_PROJEKT_NAZIV_PROJEKT").IsUnique();

                entity.Property(e => e.IdProjekt)
                    .ValueGeneratedNever()
                    .HasColumnName("ID_projekt");
                entity.Property(e => e.Cilj)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("cilj");
                entity.Property(e => e.IdVrsteProjekta).HasColumnName("id_vrste_projekta");
                entity.Property(e => e.Kratica)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("kratica");
                entity.Property(e => e.NazivProjekt)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("naziv_projekt");

                entity.HasOne(d => d.IdVrsteProjektaNavigation).WithMany(p => p.Projekti)
                    .HasForeignKey(d => d.IdVrsteProjekta)
                    .HasConstraintName("FK_projekt_vrsta_projekta");
            });

            modelBuilder.Entity<Suradnik>(entity =>
            {
                entity.HasKey(e => e.IdSuradnik).HasName("PK__SURADNIK__389BD220DB7EF845");

                entity.ToTable("SURADNIK");

                entity.Property(e => e.IdSuradnik)
                    .ValueGeneratedNever()
                    .HasColumnName("ID_suradnik");
                entity.Property(e => e.IdVrsteSur).HasColumnName("id_vrste_sur");

                entity.HasOne(d => d.IdSuradnikNavigation).WithOne(p => p.Suradnik)
                    .HasForeignKey<Suradnik>(d => d.IdSuradnik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__SURADNIK__ID_sur__71D1E811");

                entity.HasOne(d => d.IdVrsteSurNavigation).WithMany(p => p.Suradniks)
                    .HasForeignKey(d => d.IdVrsteSur)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__SURADNIK__id_vrs__72C60C4A");
            });

            modelBuilder.Entity<Trajanje>(entity =>
            {
                entity
                    .HasNoKey()
                    .ToTable("TRAJANJE");

                entity.Property(e => e.DatumPoc).HasColumnName("datum_poc");
                entity.Property(e => e.DatumKraj).HasColumnName("datum_kraj");
                entity.Property(e => e.IdProjekt).HasColumnName("ID_projekt");

                entity.HasOne(d => d.IdProjektNavigation).WithMany()
                    .HasForeignKey(d => d.IdProjekt)
                    .HasConstraintName("FK__TRAJANJE__ID_pro__778AC167");
            });

            modelBuilder.Entity<Transakcija>(entity =>
            {
                entity.HasKey(e => e.IdTransakcija).HasName("PK__TRANSAKC__8B3A732F3236EB6D");

                entity.ToTable("TRANSAKCIJA");

                entity.Property(e => e.IdTransakcija)
                    .ValueGeneratedNever()
                    .HasColumnName("ID_transakcija");
                entity.Property(e => e.BrRacuna).HasColumnName("br_racuna");
                entity.Property(e => e.IbanKorisnik)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("IBAN_korisnik");
                entity.Property(e => e.IdVrstaTrans).HasColumnName("id_vrsta_trans");
                entity.Property(e => e.Iznos)
                    .HasColumnType("numeric(10, 2)")
                    .HasColumnName("iznos");
                entity.Property(e => e.OpisTrans)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("opis_trans");
                entity.Property(e => e.Vrijeme).HasColumnName("vrijeme");

                entity.HasOne(d => d.BrRacunaNavigation).WithMany(p => p.Transakcijas)
                    .HasForeignKey(d => d.BrRacuna)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__TRANSAKCI__br_ra__06CD04F7");

                entity.HasOne(d => d.IbanKorisnikNavigation).WithMany(p => p.Transakcijas)
                    .HasForeignKey(d => d.IbanKorisnik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__TRANSAKCI__IBAN___08B54D69");

                entity.HasOne(d => d.IdVrstaTransNavigation).WithMany(p => p.Transakcijas)
                    .HasForeignKey(d => d.IdVrstaTrans)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__TRANSAKCI__id_vr__07C12930");
            });

            modelBuilder.Entity<Voditelj>(entity =>
            {
                entity.HasKey(e => e.IdSuradnik).HasName("PK__VODITELJ__389BD220AB449C48");

                entity.ToTable("VODITELJ");

                entity.Property(e => e.IdSuradnik)
                    .ValueGeneratedNever()
                    .HasColumnName("ID_suradnik");
                entity.Property(e => e.IdProjekt).HasColumnName("ID_projekt");

                entity.HasOne(d => d.IdProjektNavigation).WithMany(p => p.Voditeljs)
                    .HasForeignKey(d => d.IdProjekt)
                    .HasConstraintName("FK__VODITELJ__ID_pro__6C190EBB");

                entity.HasOne(d => d.IdSuradnikNavigation).WithOne(p => p.Voditelj)
                    .HasForeignKey<Voditelj>(d => d.IdSuradnik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__VODITELJ__ID_sur__6B24EA82");
            });

            modelBuilder.Entity<VrstaDoc>(entity =>
            {
                entity.HasKey(e => e.IdVrsteDoc).HasName("PK__VRSTA_DO__EDBF55647FE80BC6");

                entity.ToTable("VRSTA_DOC");

                entity.Property(e => e.IdVrsteDoc).HasColumnName("id_vrste_doc");
                entity.Property(e => e.NazivVrstaDoc)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_vrsta_doc");
            });

            modelBuilder.Entity<VrstaProjekta>(entity =>
            {
                entity.HasKey(e => e.IdVrsteProjekta).HasName("PK__vrsta_pr__579AF04125209EBF");

                entity.ToTable("VRSTA_PROJEKTA");

                entity.Property(e => e.IdVrsteProjekta).HasColumnName("id_vrste_projekta");
                entity.Property(e => e.NazivVrsteProjekta)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("naziv_vrste_projekta");
            });

            modelBuilder.Entity<VrstaSuradnika>(entity =>
            {
                entity.HasKey(e => e.IdVrsteSur).HasName("PK__VRSTA_SU__48F798AE161B4FF5");

                entity.ToTable("VRSTA_SURADNIKA");

                entity.Property(e => e.IdVrsteSur)
                    .ValueGeneratedNever()
                    .HasColumnName("id_vrste_sur");
                entity.Property(e => e.NazivVrstaSuradnik)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_vrsta_suradnik");
            });

            modelBuilder.Entity<VrstaTran>(entity =>
            {
                entity.HasKey(e => e.IdVrstaTrans).HasName("PK__VRSTA_TR__11109F161C01FCDA");

                entity.ToTable("VRSTA_TRANS");

                entity.Property(e => e.IdVrstaTrans)
                    .ValueGeneratedNever()
                    .HasColumnName("id_vrsta_trans");
                entity.Property(e => e.NazivVrstaTrans)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_vrsta_trans");
            });

            modelBuilder.Entity<VrstaZadatka>(entity =>
            {
                entity.HasKey(e => e.IdVrstaZad).HasName("PK__VRSTA_ZA__AFC8F557E2B4DAFB");

                entity.ToTable("VRSTA_ZADATKA");

                entity.Property(e => e.IdVrstaZad)
                    .ValueGeneratedNever()
                    .HasColumnName("id_vrsta_zad");
                entity.Property(e => e.NazivVrstaZad)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_vrsta_zad");
            });

            modelBuilder.Entity<VrstaZahtjeva>(entity =>
            {
                entity.HasKey(e => e.IdVrstaZah).HasName("PK__VRSTA_ZA__AFC8F55B5D21971E");

                entity.ToTable("VRSTA_ZAHTJEVA");

                entity.Property(e => e.IdVrstaZah)
                    .ValueGeneratedNever()
                    .HasColumnName("id_vrsta_zah");
                entity.Property(e => e.NazivVrstaZah)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("naziv_vrsta_zah");
            });

            modelBuilder.Entity<Zadatak>(entity =>
            {
                entity.HasKey(e => e.IdZad).HasName("PK__ZADATAK__689D8A8367002D85");

                entity.ToTable("ZADATAK");

                entity.Property(e => e.IdZad)
                    .ValueGeneratedNever()
                    .HasColumnName("id_zad");
                entity.Property(e => e.IdVrstaZad).HasColumnName("id_vrsta_zad");
                entity.Property(e => e.IdZah).HasColumnName("id_zah");
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("status");
                entity.Property(e => e.Trajanje).HasColumnName("trajanje");

                entity.HasOne(d => d.IdVrstaZadNavigation).WithMany(p => p.Zadataks)
                    .HasForeignKey(d => d.IdVrstaZad)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ZADATAK__id_vrst__10566F31");

                entity.HasOne(d => d.IdZahNavigation).WithMany(p => p.Zadataks)
                    .HasForeignKey(d => d.IdZah)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ZADATAK__id_zah__0F624AF8");
            });

            modelBuilder.Entity<Zahtijev>(entity =>
            {
                entity.HasKey(e => e.IdZah).HasName("PK__ZAHTIJEV__689D8A87DC43C235");

                entity.ToTable("ZAHTIJEV");

                entity.Property(e => e.IdZah)
                    .ValueGeneratedNever()
                    .HasColumnName("id_zah");
                entity.Property(e => e.IdSuradnik).HasColumnName("ID_suradnik");
                entity.Property(e => e.IdVrstaZah).HasColumnName("id_vrsta_zah");
                entity.Property(e => e.OpisZahtijev)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("opis_zahtjev");
                entity.Property(e => e.Prioritet)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("prioritet");

                entity.HasOne(d => d.IdSuradnikNavigation).WithMany(p => p.Zahtijevs)
                    .HasForeignKey(d => d.IdSuradnik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ZAHTIJEV__ID_sur__0C85DE4D");

                entity.HasOne(d => d.IdVrstaZahNavigation).WithMany(p => p.Zahtijevs)
                    .HasForeignKey(d => d.IdVrstaZah)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ZAHTIJEV__id_vrs__0B91BA14");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            base.ConfigureConventions(builder);
            builder.Properties<DateOnly>()
                .HaveConversion<DateOnlyConverter>();
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}