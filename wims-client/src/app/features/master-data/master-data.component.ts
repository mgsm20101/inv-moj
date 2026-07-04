import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { problemDetail } from '../../core/http/problem-details';
import { CategoryDto, UnitDto } from '../../core/models/catalog.models';
import {
  EMPLOYEE_STATUS_BADGE,
  EMPLOYEE_STATUS_LABEL,
  EmployeeDto,
  EmployeeStatus,
} from '../../core/models/custody.models';
import { WarehouseDto } from '../../core/models/voucher.models';
import { HasPermissionDirective } from '../../shared/has-permission.directive';
import { SupplierDto, WAREHOUSE_TYPE_LABEL, WarehouseType } from './master-data.models';
import { MasterDataService } from './master-data.service';
import { CategoryEditorComponent } from './categories/category-editor.component';
import { EmployeeEditorComponent } from './employees/employee-editor.component';
import { SupplierEditorComponent } from './suppliers/supplier-editor.component';
import { UnitEditorComponent } from './units/unit-editor.component';
import { WarehouseEditorComponent } from './warehouses/warehouse-editor.component';

type Tab = 'warehouses' | 'employees' | 'categories' | 'units' | 'suppliers';

/**
 * البيانات الأساسية — شاشة موحّدة بتبويبات لإدارة الكيانات المرجعية:
 * المخازن، الموظفون، التصنيفات، وحدات القياس (عرض + إضافة + تعديل).
 * التبويبات والأزرار محكومة بصلاحيات RBAC. النمط يطابق شاشة الإدارة.
 */
@Component({
  selector: 'wims-master-data',
  standalone: true,
  imports: [
    HasPermissionDirective,
    WarehouseEditorComponent,
    EmployeeEditorComponent,
    CategoryEditorComponent,
    UnitEditorComponent,
    SupplierEditorComponent,
  ],
  templateUrl: './master-data.component.html',
  styleUrl: './master-data.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MasterDataComponent {
  private readonly auth = inject(AuthService);
  private readonly service = inject(MasterDataService);

  // ---- الصلاحيات ----
  readonly canViewWarehouses = this.auth.hasPermission('Warehouses.View');
  readonly canManageWarehouses = this.auth.hasPermission('Warehouses.Manage');
  readonly canViewEmployees = this.auth.hasPermission('Employees.View');
  readonly canManageEmployees = this.auth.hasPermission('Employees.Manage');
  readonly canViewCatalog = this.auth.hasPermission('Items.View');
  readonly canManageCatalog = this.auth.hasPermission('Items.Manage');
  readonly canViewSuppliers = this.auth.hasPermission('Suppliers.View');
  readonly canManageSuppliers = this.auth.hasPermission('Suppliers.Manage');

  readonly tab = signal<Tab>(
    this.canViewWarehouses
      ? 'warehouses'
      : this.canViewEmployees
        ? 'employees'
        : this.canViewCatalog
          ? 'categories'
          : 'suppliers',
  );

  constructor() {
    this.loadTab(this.tab());
  }

  setTab(t: Tab): void {
    this.tab.set(t);
    this.loadTab(t);
  }

  private loadTab(t: Tab): void {
    if (t === 'warehouses' && this.warehouses() === null) this.loadWarehouses();
    if (t === 'employees' && this.employees() === null) this.loadEmployees();
    if (t === 'categories' && this.categories() === null) this.loadCategories();
    if (t === 'units' && this.units() === null) this.loadUnits();
    if (t === 'suppliers' && this.suppliers() === null) this.loadSuppliers();
  }

  // ---- ملصقات مساعدة ----
  warehouseTypeLabel(t: number): string {
    return WAREHOUSE_TYPE_LABEL[t as WarehouseType] ?? '—';
  }
  empStatusLabel(s: EmployeeStatus): string {
    return EMPLOYEE_STATUS_LABEL[s];
  }
  empStatusBadge(s: EmployeeStatus): string {
    return EMPLOYEE_STATUS_BADGE[s];
  }

  // ---- المخازن ----
  readonly warehouses = signal<WarehouseDto[] | null>(null);
  readonly warehousesLoading = signal(false);
  readonly warehousesError = signal<string | null>(null);
  readonly editingWarehouse = signal<WarehouseDto | null | undefined>(undefined);

  loadWarehouses(): void {
    this.warehousesLoading.set(true);
    this.warehousesError.set(null);
    this.service.getWarehouses().subscribe({
      next: (w) => {
        this.warehouses.set(w);
        this.warehousesLoading.set(false);
      },
      error: (e) => {
        this.warehousesError.set(problemDetail(e, 'تعذّر تحميل المخازن.'));
        this.warehousesLoading.set(false);
      },
    });
  }
  openNewWarehouse(): void {
    this.editingWarehouse.set(null);
  }
  openEditWarehouse(w: WarehouseDto): void {
    this.editingWarehouse.set(w);
  }
  closeWarehouseEditor(saved: boolean): void {
    this.editingWarehouse.set(undefined);
    if (saved) this.loadWarehouses();
  }

  // ---- الموظفون ----
  readonly employees = signal<EmployeeDto[] | null>(null);
  readonly employeesLoading = signal(false);
  readonly employeesError = signal<string | null>(null);
  readonly editingEmployee = signal<EmployeeDto | null | undefined>(undefined);

  loadEmployees(): void {
    this.employeesLoading.set(true);
    this.employeesError.set(null);
    this.service.getEmployees().subscribe({
      next: (e) => {
        this.employees.set(e);
        this.employeesLoading.set(false);
      },
      error: (e) => {
        this.employeesError.set(problemDetail(e, 'تعذّر تحميل الموظفين.'));
        this.employeesLoading.set(false);
      },
    });
  }
  openNewEmployee(): void {
    this.editingEmployee.set(null);
  }
  openEditEmployee(e: EmployeeDto): void {
    this.editingEmployee.set(e);
  }
  closeEmployeeEditor(saved: boolean): void {
    this.editingEmployee.set(undefined);
    if (saved) this.loadEmployees();
  }

  // ---- التصنيفات ----
  readonly categories = signal<CategoryDto[] | null>(null);
  readonly categoriesLoading = signal(false);
  readonly categoriesError = signal<string | null>(null);
  readonly editingCategory = signal<CategoryDto | null | undefined>(undefined);

  loadCategories(): void {
    this.categoriesLoading.set(true);
    this.categoriesError.set(null);
    this.service.getCategories().subscribe({
      next: (c) => {
        this.categories.set(c);
        this.categoriesLoading.set(false);
      },
      error: (e) => {
        this.categoriesError.set(problemDetail(e, 'تعذّر تحميل التصنيفات.'));
        this.categoriesLoading.set(false);
      },
    });
  }
  openNewCategory(): void {
    this.editingCategory.set(null);
  }
  openEditCategory(c: CategoryDto): void {
    this.editingCategory.set(c);
  }
  closeCategoryEditor(saved: boolean): void {
    this.editingCategory.set(undefined);
    if (saved) this.loadCategories();
  }

  // ---- وحدات القياس ----
  readonly units = signal<UnitDto[] | null>(null);
  readonly unitsLoading = signal(false);
  readonly unitsError = signal<string | null>(null);
  readonly editingUnit = signal<UnitDto | null | undefined>(undefined);

  loadUnits(): void {
    this.unitsLoading.set(true);
    this.unitsError.set(null);
    this.service.getUnits().subscribe({
      next: (u) => {
        this.units.set(u);
        this.unitsLoading.set(false);
      },
      error: (e) => {
        this.unitsError.set(problemDetail(e, 'تعذّر تحميل الوحدات.'));
        this.unitsLoading.set(false);
      },
    });
  }
  openNewUnit(): void {
    this.editingUnit.set(null);
  }
  openEditUnit(u: UnitDto): void {
    this.editingUnit.set(u);
  }
  closeUnitEditor(saved: boolean): void {
    this.editingUnit.set(undefined);
    if (saved) this.loadUnits();
  }

  // ---- الموردون ----
  readonly suppliers = signal<SupplierDto[] | null>(null);
  readonly suppliersLoading = signal(false);
  readonly suppliersError = signal<string | null>(null);
  readonly editingSupplier = signal<SupplierDto | null | undefined>(undefined);

  loadSuppliers(): void {
    this.suppliersLoading.set(true);
    this.suppliersError.set(null);
    this.service.getSuppliers().subscribe({
      next: (s) => {
        this.suppliers.set(s);
        this.suppliersLoading.set(false);
      },
      error: (e) => {
        this.suppliersError.set(problemDetail(e, 'تعذّر تحميل الموردين.'));
        this.suppliersLoading.set(false);
      },
    });
  }
  openNewSupplier(): void {
    this.editingSupplier.set(null);
  }
  openEditSupplier(s: SupplierDto): void {
    this.editingSupplier.set(s);
  }
  closeSupplierEditor(saved: boolean): void {
    this.editingSupplier.set(undefined);
    if (saved) this.loadSuppliers();
  }
}
