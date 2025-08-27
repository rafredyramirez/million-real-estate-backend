// 00-init-realestate.js

(function () {
  var log  = function (m) { print("[INIT] " + m); };
  var warn = function (m) { print("[INIT][WARN] " + m); };
  var err  = function (m, e) {
    var msg = (e && e.message) ? e.message : e;
    print("[INIT][ERROR] " + m + ": " + msg);
  };

  var dbReal = db.getSiblingDB("realestate");

  // ---------- 1) Usuario de aplicación ----------
  try {
    dbReal.createUser({
      user: "realestate_app",
      pwd: "changeme",
      roles: [{ role: "readWrite", db: "realestate" }]
    });
    log("Usuario realestate_app creado");
  } catch (e) {
    if (e && (e.codeName === "DuplicateKey" || /already exists/i.test(e.message || ""))) {
      log("Usuario realestate_app ya existía");
    } else {
      err("Creando usuario realestate_app", e);
    }
  }

  // ---------- Helpers ----------
  function collectionExists(name) {
    try {
      var res = dbReal.runCommand({ listCollections: 1, filter: { name: name }, nameOnly: true });
      return !!(res.ok && res.cursor && res.cursor.firstBatch && res.cursor.firstBatch.length === 1);
    } catch (e) {
      warn("No se pudo listar colecciones (asumimos que no existe " + name + ")");
      return false;
    }
  }

  function ensureCollection(name, validator) {
    try {
      if (!collectionExists(name)) {
        dbReal.createCollection(name, {
          validator: validator,
          validationLevel: "strict",
          validationAction: "error"
        });
        log("Colección creada: " + name);
      } else {
        try {
          dbReal.runCommand({
            collMod: name,
            validator: validator,
            validationLevel: "strict",
            validationAction: "error"
          });
          log("Validador actualizado: " + name);
        } catch (e) {
          warn("collMod falló en " + name + ". Continuamos. Detalle: " + (e && e.message ? e.message : e));
        }
      }
    } catch (e) {
      err("Creando/actualizando colección " + name, e);
    }
  }

  // ---------- 2) Validadores ----------
  var ownerValidator = {
    $jsonSchema: {
      bsonType: "object",
      required: ["Name"],
      additionalProperties: true,
      properties: {
        _id: { bsonType: "objectId" },
        Name: { bsonType: "string", minLength: 1 },
        Address: { bsonType: ["string", "null"] },
        Photo: { bsonType: ["string", "null"] },
        Birthday: { bsonType: ["date", "null"] }
      }
    }
  };

  var propertyValidator = {
    $jsonSchema: {
      bsonType: "object",
      required: ["IdOwner", "Name", "Address", "Price", "CodeInternal", "Year", "CreatedAt", "UpdatedAt"],
      additionalProperties: true,
      properties: {
        _id: { bsonType: "objectId" },
        IdOwner: { bsonType: "objectId" },
        Name: { bsonType: "string", minLength: 1 },
        Address: { bsonType: "string", minLength: 1 },
        Price: { bsonType: ["decimal", "double", "int", "long"] },
        CodeInternal: { bsonType: "string", minLength: 1 },
        Year: { bsonType: "int", minimum: 1800, maximum: 3000 },
        CreatedAt: { bsonType: "date" },
        UpdatedAt: { bsonType: "date" }
      }
    }
  };

  var propertyImageValidator = {
    $jsonSchema: {
      bsonType: "object",
      required: ["IdProperty", "File", "Enabled"],
      additionalProperties: true,
      properties: {
        _id: { bsonType: "objectId" },
        IdProperty: { bsonType: "objectId" },
        File: { bsonType: "string", minLength: 1 },
        Enabled: { bsonType: "bool" }
      }
    }
  };

  var propertyTraceValidator = {
    $jsonSchema: {
      bsonType: "object",
      required: ["IdProperty", "DateSale", "Name", "Value", "Tax"],
      additionalProperties: true,
      properties: {
        _id: { bsonType: "objectId" },
        IdProperty: { bsonType: "objectId" },
        DateSale: { bsonType: "date" },
        Name: { bsonType: "string", minLength: 1 },
        Value: { bsonType: ["decimal", "double", "int", "long"] },
        Tax: { bsonType: ["decimal", "double", "int", "long"] }
      }
    }
  };

  // ---------- 3) Colecciones ----------
  ensureCollection("Owners", ownerValidator);
  ensureCollection("Properties", propertyValidator);
  ensureCollection("PropertyImages", propertyImageValidator);
  ensureCollection("PropertyTraces", propertyTraceValidator);

  // ---------- 4) Índices ----------
  try {
    dbReal.Owners.createIndexes([{ key: { Name: "text" }, name: "tx_name" }]);
  } catch (e) { warn("Índices Owners: " + (e && e.message ? e.message : e)); }

  try {
    dbReal.Properties.createIndexes([
      { key: { Name: "text", Address: "text" }, name: "tx_name_address" },
      { key: { CodeInternal: 1 }, name: "uk_codeinternal", unique: true },
      { key: { Price: 1 }, name: "ix_price" },
      { key: { IdOwner: 1 }, name: "ix_owner" },
      { key: { CreatedAt: -1 }, name: "ix_createdAt_desc" }
    ]);
  } catch (e) { warn("Índices Properties: " + (e && e.message ? e.message : e)); }

  try {
    dbReal.PropertyImages.createIndexes([
      { key: { IdProperty: 1, Enabled: 1 }, name: "ix_property_enabled" }
    ]);
  } catch (e) { warn("Índices PropertyImages: " + (e && e.message ? e.message : e)); }

  try {
    dbReal.PropertyTraces.createIndexes([
      { key: { IdProperty: 1, DateSale: -1 }, name: "ix_property_datesale_desc" }
    ]);
  } catch (e) { warn("Índices PropertyTraces: " + (e && e.message ? e.message : e)); }

  log("Estructura 'realestate' lista (usuario, colecciones, índices).");
})();
