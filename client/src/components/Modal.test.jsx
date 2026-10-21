import { render,screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {vi,it,expect} from 'vitest';
import {Modal} from './Common';
it('hỗ trợ đóng bằng Escape và trả lại scroll',async()=>{
  const close=vi.fn();const {unmount}=render(<Modal title="Đặt lịch" onClose={close}><input aria-label="Lời nhắn"/></Modal>);
  expect(screen.getByRole('dialog')).toBeVisible();
  expect(document.body.style.overflow).toBe('hidden');
  await userEvent.keyboard('{Escape}');
  expect(close).toHaveBeenCalled();
  unmount();expect(document.body.style.overflow).toBe('');
});
it('giữ focus khi gõ trong form',async()=>{
  render(<Modal title="Lời nhắn" onClose={()=>{}}><input aria-label="Ghi chú"/></Modal>);
  await userEvent.type(screen.getByLabelText('Ghi chú'),'Muốn xem phòng');
  expect(screen.getByLabelText('Ghi chú')).toHaveValue('Muốn xem phòng');
});
