import { SuvodaProductType, ModuleType } from "@/types/product-types"

export async function seedSampleProducts() {
  // Sample Trial Manager products
  const tmProducts = [
    {
      id: crypto.randomUUID(),
      name: "Trial Manager Alpha",
      version: "1.0.0",
      productType: SuvodaProductType.TrialManager,
    },
    {
      id: crypto.randomUUID(),
      name: "Trial Manager Beta",
      version: "2.0.0",
      productType: SuvodaProductType.TrialManager,
    },
  ]

  // Sample IRT products
  const irtProducts = [
    {
      id: crypto.randomUUID(),
      name: "IRT System A",
      version: "1.2.0",
      productType: SuvodaProductType.IRT,
      modules: [
        {
          id: crypto.randomUUID(),
          name: "Randomization Module",
          version: "1.0.0",
          productType: SuvodaProductType.Module,
          moduleType: ModuleType.Randomization,
        },
        {
          id: crypto.randomUUID(),
          name: "Drug Supply Module",
          version: "1.1.0",
          productType: SuvodaProductType.Module,
          moduleType: ModuleType.DrugSupply,
        },
      ],
    },
    {
      id: crypto.randomUUID(),
      name: "IRT System B",
      version: "2.1.0",
      productType: SuvodaProductType.IRT,
      modules: [
        {
          id: crypto.randomUUID(),
          name: "Subject Management Module",
          version: "1.0.0",
          productType: SuvodaProductType.Module,
          moduleType: ModuleType.SubjectManagement,
        },
      ],
    },
  ]

  // Sample eCOA products
  const ecoaProducts = [
    {
      id: crypto.randomUUID(),
      name: "eCOA Platform",
      version: "1.0.0",
      productType: SuvodaProductType.eCOA,
    },
  ]

  // Save products to database
  // This is a simplified example - in a real app, you'd use your repository methods
  console.log("Sample products seeded successfully")

  return {
    tmProducts,
    irtProducts,
    ecoaProducts,
  }
}
