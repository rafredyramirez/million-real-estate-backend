// 10-seed-extra.js — Inserta propiedades si no existen (por CodeInternal)
(function () {
  var log = function (m) { print("[EXTRA] " + m); };
  var dbReal = db.getSiblingDB("realestate");
  var now = new Date();

  function ensureOwnerByName(name, address) {
    var found = dbReal.Owners.findOne({ Name: name });
    if (found) return found._id;
    return dbReal.Owners.insertOne({ Name: name, Address: address || null, Photo: null, Birthday: null }).insertedId;
  }

  var items = [
    { ownerName: "Inmobiliaria Andes", ownerAddr: "Av. Libertador 99", Name: "Apartamento Centro", Address: "Cra 7 # 45-21", Price: "420000", CodeInternal: "P-0002", Year: 2018, Images: ["https://picsum.photos/id/100/800/600"] },
    { ownerName: "María Pérez",        ownerAddr: "Calle 80 #20-15",  Name: "Estudio Chapinero",  Address: "Cl 54 # 9-12",  Price: "180000", CodeInternal: "P-0003", Year: 2012, Images: ["https://picsum.photos/id/103/800/600","https://picsum.photos/id/104/800/600"] },
    { ownerName: "Grupo Hábitat",      ownerAddr: "Carrera 7 #120-30", Name: "Casa Campestre",   Address: "Vereda El Retiro", Price: "560000", CodeInternal: "P-0004", Year: 2021, Images: ["https://picsum.photos/id/1056/800/600"] },
    { ownerName: "Grupo Hábitat",      ownerAddr: "Carrera 7 #120-30", Name: "Loft Zona T",     Address: "Cl 82 # 13-20",  Price: "230000", CodeInternal: "P-0005", Year: 2016, Images: ["https://picsum.photos/id/1069/800/600"] },
    { ownerName: "Inmobiliaria Andes", ownerAddr: "Av. Libertador 99", Name: "Dúplex Salitre",   Address: "Av 68 # 24-80",  Price: "310000", CodeInternal: "P-0006", Year: 2014, Images: ["https://picsum.photos/id/1080/800/600"] },
    { ownerName: "Carlos López",       ownerAddr: "Cll 26 # 45-10",    Name: "Penthouse Chicó", Address: "Cl 93 # 11-45",  Price: "790000", CodeInternal: "P-0007", Year: 2023, Images: ["https://picsum.photos/id/1084/800/600"] },
    { ownerName: "Inmobiliaria Andes", ownerAddr: "Av. Libertador 99", Name: "Oficina Pequeña", Address: "Cl 72 # 10-12",  Price: "145000", CodeInternal: "P-0008", Year: 2010, Images: ["https://picsum.photos/id/109/800/600"] },
    { ownerName: "María Pérez",        ownerAddr: "Calle 80 #20-15",   Name: "Casa Suba",       Address: "Cl 139 # 95-20", Price: "270000", CodeInternal: "P-0009", Year: 2013, Images: ["https://picsum.photos/id/110/800/600"] },
    { ownerName: "Grupo Hábitat",      ownerAddr: "Carrera 7 #120-30", Name: "Apto Cedritos",   Address: "Cl 140 # 19-40", Price: "380000", CodeInternal: "P-0010", Year: 2019, Images: ["https://picsum.photos/id/111/800/600"] },
    { ownerName: "Carlos López",       ownerAddr: "Cll 26 # 45-10",    Name: "Casa Usaquén",    Address: "Cra 7 # 119-30", Price: "500000", CodeInternal: "P-0011", Year: 2017, Images: ["https://picsum.photos/id/112/800/600"] },
    { ownerName: "Carlos López",       ownerAddr: "Cll 26 # 45-10",    Name: "Apto Belén",     Address: "Cl 30 # 74-10, Medellín", Price: "210000", CodeInternal: "P-0012", Year: 2011, Images: [] }
  ];

  var inserted = 0;
  for (var i = 0; i < items.length; i++) {
    var it = items[i];
    if (dbReal.Properties.findOne({ CodeInternal: it.CodeInternal })) {
      continue;
    }
    var ownerId = ensureOwnerByName(it.ownerName, it.ownerAddr);
    var propId = dbReal.Properties.insertOne({
      IdOwner: ownerId,
      Name: it.Name,
      Address: it.Address,
      Price: NumberDecimal(it.Price),
      CodeInternal: it.CodeInternal,
      Year: it.Year,
      CreatedAt: now,
      UpdatedAt: now
    }).insertedId;

    if (it.Images && it.Images.length) {
      dbReal.PropertyImages.insertOne({ IdProperty: propId, File: it.Images[0], Enabled: true });
      if (it.Images.length > 1) {
        var rest = [];
        for (var j = 1; j < it.Images.length; j++) {
          rest.push({ IdProperty: propId, File: it.Images[j], Enabled: false });
        }
        if (rest.length) dbReal.PropertyImages.insertMany(rest);
      }
    }
    inserted++;
  }
  log("Insertados " + inserted + " registros nuevos en Properties");
})();
