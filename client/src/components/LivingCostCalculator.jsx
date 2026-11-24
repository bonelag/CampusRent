import React, { useState } from 'react';

const LivingCostCalculator = ({ defaultRent = 2500000 }) => {
  const [roomRent, setRoomRent] = useState(defaultRent);
  const [electricityKwh, setElectricityKwh] = useState(80);
  const [electricityRate, setElectricityRate] = useState(3500);
  const [waterM3, setWaterM3] = useState(5);
  const [waterRate, setWaterRate] = useState(20000);
  const [serviceFee, setServiceFee] = useState(150000);

  const electricityCost = Number(electricityKwh || 0) * Number(electricityRate || 0);
  const waterCost = Number(waterM3 || 0) * Number(waterRate || 0);
  const totalCost =
    Number(roomRent || 0) + electricityCost + waterCost + Number(serviceFee || 0);

  const formatVND = (amount) => amount.toLocaleString('vi-VN') + ' đ';

  return (
    <div className="w-full max-w-lg mx-auto bg-white rounded-2xl shadow-md border border-gray-100 p-5 sm:p-6">
      <div className="mb-5">
        <h3 className="text-lg sm:text-xl font-bold text-gray-900">
          Dự toán chi phí sinh hoạt / tháng
        </h3>
        <p className="text-sm text-gray-500 mt-1">
          Nhập mức sử dụng điện, nước dự kiến để tính tổng tiền phòng thực tế
        </p>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Tiền thuê phòng (VNĐ/tháng)
          </label>
          <input
            type="number"
            min="0"
            value={roomRent}
            onChange={(e) => setRoomRent(e.target.value)}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Số điện tiêu thụ (kWh)
            </label>
            <input
              type="number"
              min="0"
              value={electricityKwh}
              onChange={(e) => setElectricityKwh(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Đơn giá điện (VNĐ/kWh)
            </label>
            <input
              type="number"
              min="0"
              value={electricityRate}
              onChange={(e) => setElectricityRate(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Số nước tiêu thụ (m³)
            </label>
            <input
              type="number"
              min="0"
              value={waterM3}
              onChange={(e) => setWaterM3(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Đơn giá nước (VNĐ/m³)
            </label>
            <input
              type="number"
              min="0"
              value={waterRate}
              onChange={(e) => setWaterRate(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Phí dịch vụ khác (Internet, rác, gửi xe...)
          </label>
          <input
            type="number"
            min="0"
            value={serviceFee}
            onChange={(e) => setServiceFee(e.target.value)}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      <div className="mt-6 pt-4 border-t border-gray-100 space-y-2 text-sm">
        <div className="flex justify-between text-gray-600">
          <span>Tiền điện ({electricityKwh || 0} kWh):</span>
          <span className="font-medium">{formatVND(electricityCost)}</span>
        </div>
        <div className="flex justify-between text-gray-600">
          <span>Tiền nước ({waterM3 || 0} m³):</span>
          <span className="font-medium">{formatVND(waterCost)}</span>
        </div>
        <div className="flex justify-between items-center pt-2 text-base font-bold text-blue-600">
          <span>Tổng cộng dự kiến:</span>
          <span className="text-lg">{formatVND(totalCost)}</span>
        </div>
      </div>
    </div>
  );
};

export default LivingCostCalculator;
